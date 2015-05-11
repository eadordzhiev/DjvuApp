//C-  -*- C++ -*-
//C- -------------------------------------------------------------------
//C- DjVuLibre-3.5
//C- Copyright (c) 2002  Leon Bottou and Yann Le Cun.
//C- Copyright (c) 2001  AT&T
//C-
//C- This software is subject to, and may be distributed under, the
//C- GNU General Public License, either Version 2 of the license,
//C- or (at your option) any later version. The license should have
//C- accompanied the software or you may obtain a copy of the license
//C- from the Free Software Foundation at http://www.fsf.org .
//C-
//C- This program is distributed in the hope that it will be useful,
//C- but WITHOUT ANY WARRANTY; without even the implied warranty of
//C- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//C- GNU General Public License for more details.
//C- 
//C- DjVuLibre-3.5 is derived from the DjVu(r) Reference Library from
//C- Lizardtech Software.  Lizardtech Software has authorized us to
//C- replace the original DjVu(r) Reference Library notice by the following
//C- text (see doc/lizard2002.djvu and doc/lizardtech2007.djvu):
//C-
//C-  ------------------------------------------------------------------
//C- | DjVu (r) Reference Library (v. 3.5)
//C- | Copyright (c) 1999-2001 LizardTech, Inc. All Rights Reserved.
//C- | The DjVu Reference Library is protected by U.S. Pat. No.
//C- | 6,058,214 and patents pending.
//C- |
//C- | This software is subject to, and may be distributed under, the
//C- | GNU General Public License, either Version 2 of the license,
//C- | or (at your option) any later version. The license should have
//C- | accompanied the software or you may obtain a copy of the license
//C- | from the Free Software Foundation at http://www.fsf.org .
//C- |
//C- | The computer code originally released by LizardTech under this
//C- | license and unmodified by other parties is deemed "the LIZARDTECH
//C- | ORIGINAL CODE."  Subject to any third party intellectual property
//C- | claims, LizardTech grants recipient a worldwide, royalty-free, 
//C- | non-exclusive license to make, use, sell, or otherwise dispose of 
//C- | the LIZARDTECH ORIGINAL CODE or of programs derived from the 
//C- | LIZARDTECH ORIGINAL CODE in compliance with the terms of the GNU 
//C- | General Public License.   This grant only confers the right to 
//C- | infringe patent claims underlying the LIZARDTECH ORIGINAL CODE to 
//C- | the extent such infringement is reasonably necessary to enable 
//C- | recipient to make, have made, practice, sell, or otherwise dispose 
//C- | of the LIZARDTECH ORIGINAL CODE (or portions thereof) and not to 
//C- | any greater extent that may be necessary to utilize further 
//C- | modifications or combinations.
//C- |
//C- | The LIZARDTECH ORIGINAL CODE is provided "AS IS" WITHOUT WARRANTY
//C- | OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//C- | TO ANY WARRANTY OF NON-INFRINGEMENT, OR ANY IMPLIED WARRANTY OF
//C- | MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
//C- +------------------------------------------------------------------

#ifdef HAVE_CONFIG_H
# include "config.h"
#endif
#if NEED_GNUG_PRAGMAS
# pragma implementation
#endif

// This file defines machine independent classes
// for running and synchronizing threads.
// - Author: Leon Bottou, 01/1998

// From: Leon Bottou, 1/31/2002
// Almost unchanged by Lizardtech.
// GSafeFlags should go because it not as safe as it claims.

#include "GThreads.h"
#include "GException.h"
#include "DjVuMessageLite.h"

#include <stddef.h>
#include <stdlib.h>
#include <stdio.h>

#ifndef _DEBUG
#if defined(DEBUG) 
#define _DEBUG /* */
#elif DEBUGLVL >= 1
#define _DEBUG /* */
#endif
#endif

#if THREADMODEL==WINTHREADS
# include <process.h>
#endif


#ifdef HAVE_NAMESPACES
namespace DJVU {
# ifdef NOT_DEFINED // Just to fool emacs c++ mode
}
#endif
#endif


// ----------------------------------------
// NOTHREADS
// ----------------------------------------

#if THREADMODEL==NOTHREADS
int
GThread::create( void (*entry)(void*), void *arg)
{
  (*entry)(arg);
  return 0;
}
#endif


// ----------------------------------------
// WIN32 IMPLEMENTATION
// ----------------------------------------

#if THREADMODEL==WINTHREADS

static unsigned __stdcall 
start(void *arg)
{
  GThread *gt = (GThread*)arg;
  try 
    {
      G_TRY
        {
          gt->xentry( gt->xarg );
        }
      G_CATCH(ex)
        {
          ex.perror();
          DjVuMessageLite::perror( ERR_MSG("GThreads.uncaught") );
#ifdef _DEBUG
          abort();
#endif
        }
      G_ENDCATCH;
    }
  catch(...)
    {
      DjVuMessageLite::perror( ERR_MSG("GThreads.unrecognized") );
#ifdef _DEBUG
      abort();
#endif
    }
  return 0;
}

GThread::GThread(int stacksize)
  : hthr(0), thrid(0), xentry(0), xarg(0)
{
}

GThread::~GThread()
{
  if (hthr)
    CloseHandle(hthr);
  hthr = 0;
  thrid = 0;
}

int  
GThread::create(void (*entry)(void*), void *arg)
{
  if (hthr)
    return -1;
  xentry = entry;
  xarg = arg;
  unsigned uthread = 0;
  hthr = (HANDLE)_beginthreadex(NULL, 0, start, (void*)this, 0, &uthread);
  thrid = (DWORD) uthread;
  if (hthr)
    return 0;
  return -1;
}

void 
GThread::terminate()
{
  OutputDebugString(TEXT("Terminating thread.\n"));
  if (hthr)
    TerminateThread(hthr,0);
}

int
GThread::yield()
{
  Sleep(0);
  return 0;
}

void *
GThread::current()
{
  return (void*) GetCurrentThreadId();
}

struct thr_waiting {
  struct thr_waiting *next;
  struct thr_waiting *prev;
  BOOL   waiting;
  HANDLE gwait;
};

GMonitor::GMonitor()
  : ok(0), count(1), head(0), tail(0)
{
  InitializeCriticalSection(&cs);
  locker = GetCurrentThreadId();
  ok = 1;
}

GMonitor::~GMonitor()
{
  ok = 0;
  EnterCriticalSection(&cs);
  for (struct thr_waiting *w=head; w; w=w->next)
    SetEvent(w->gwait);
  LeaveCriticalSection(&cs);
  DeleteCriticalSection(&cs); 
}

void 
GMonitor::enter()
{
  DWORD self = GetCurrentThreadId();
  if (count>0 || self!=locker)
    {
      if (ok)
        EnterCriticalSection(&cs);
      locker = self;
      count = 1;
    }
  count -= 1;
}

void 
GMonitor::leave()
{
  DWORD self = GetCurrentThreadId();
  if (ok && (count>0 || self!=locker))
    G_THROW( ERR_MSG("GThreads.not_acq_broad") );
  count += 1;
  if (count > 0)
    {
      count = 1;
      if (ok)
        LeaveCriticalSection(&cs);
    }
}

void
GMonitor::signal()
{
  if (ok)
    {
      DWORD self = GetCurrentThreadId();
      if (count>0 || self!=locker)
        G_THROW( ERR_MSG("GThreads.not_acq_signal") );
      for (struct thr_waiting *w=head; w; w=w->next)
        if (w->waiting) 
          {
            SetEvent(w->gwait);
            w->waiting = FALSE;
            break; // Only one thread is allowed to run!
          }
    }
}

void
GMonitor::broadcast()
{
  if (ok)
    {
      DWORD self = GetCurrentThreadId();
      if (count>0 || self!=locker)
        G_THROW( ERR_MSG("GThreads.not_acq_broad") );
      for (struct thr_waiting *w=head; w; w=w->next)
        if (w->waiting)
            {
              SetEvent(w->gwait);
              w->waiting = FALSE;
            }
    }
}

void
GMonitor::wait()
{
  // Check state
  DWORD self = GetCurrentThreadId();
  if (count>0 || self!=locker)
    G_THROW( ERR_MSG("GThreads.not_acq_wait") );
  // Wait
  if (ok)
    {
      // Prepare wait record
      struct thr_waiting waitrec;
      waitrec.waiting = TRUE;
      waitrec.gwait = CreateEvent(NULL,FALSE,FALSE,NULL);
      waitrec.next = 0;
      waitrec.prev = tail;
      // Link wait record (protected by critical section)
      *(waitrec.next ? &waitrec.next->prev : &tail) = &waitrec; 
      *(waitrec.prev ? &waitrec.prev->next : &head) = &waitrec;
      // Start wait
      int sav_count = count;
      count = 1;
      LeaveCriticalSection(&cs);
      WaitForSingleObject(waitrec.gwait,INFINITE);
      // Re-acquire
      EnterCriticalSection(&cs);
      count = sav_count;
      locker = self;
      // Unlink wait record
      *(waitrec.next ? &waitrec.next->prev : &tail) = waitrec.prev;
      *(waitrec.prev ? &waitrec.prev->next : &head) = waitrec.next;
      CloseHandle(waitrec.gwait);
    }
}

void
GMonitor::wait(unsigned long timeout) 
{
  // Check state
  DWORD self = GetCurrentThreadId();
  if (count>0 || self!=locker)
    G_THROW( ERR_MSG("GThreads.not_acq_wait") );
  // Wait
  if (ok)
    {
      // Prepare wait record
      struct thr_waiting waitrec;
      waitrec.waiting = TRUE;
      waitrec.gwait = CreateEvent(NULL,FALSE,FALSE,NULL);
      waitrec.next = 0;
      waitrec.prev = tail;
      // Link wait record (protected by critical section)
      *(waitrec.prev ? &waitrec.prev->next : &head) = &waitrec;
      *(waitrec.next ? &waitrec.next->prev : &tail) = &waitrec; 
      // Start wait
      int sav_count = count;
      count = 1;
      LeaveCriticalSection(&cs);
      WaitForSingleObject(waitrec.gwait,timeout);
      // Re-acquire
      EnterCriticalSection(&cs);
      count = sav_count;
      locker = self;
      // Unlink wait record
      *(waitrec.next ? &waitrec.next->prev : &tail) = waitrec.prev;
      *(waitrec.prev ? &waitrec.prev->next : &head) = waitrec.next;
      CloseHandle(waitrec.gwait);
    }
}

#endif


// ----------------------------------------
// GSAFEFLAGS 
// ----------------------------------------



GSafeFlags &
GSafeFlags::operator=(long xflags)
{
   enter();
   if (flags!=xflags)
   {
      flags=xflags;
      broadcast();
   }
   leave();
   return *this;
}

GSafeFlags::operator long(void) const
{
   long f;
   ((GSafeFlags *) this)->enter();
   f=flags;
   ((GSafeFlags *) this)->leave();
   return f;
}

bool
GSafeFlags::test_and_modify(long set_mask, long clr_mask,
			    long set_mask1, long clr_mask1)
{
   enter();
   if ((flags & set_mask)==set_mask &&
       (~flags & clr_mask)==clr_mask)
   {
      long new_flags=flags;
      new_flags|=set_mask1;
      new_flags&=~clr_mask1;
      if (new_flags!=flags)
      {
	 flags=new_flags;
	 broadcast();
      }
      leave();
      return true;
   }
   leave();
   return false;
}

void
GSafeFlags::wait_and_modify(long set_mask, long clr_mask,
			    long set_mask1, long clr_mask1)
{
   enter();
   while((flags & set_mask)!=set_mask ||
	 (~flags & clr_mask)!=clr_mask) wait();
   long new_flags=flags;
   new_flags|=set_mask1;
   new_flags&=~clr_mask1;
   if (flags!=new_flags)
   {
      flags=new_flags;
      broadcast();
   }
   leave();
}


// ----------------------------------------
// WINRT IMPLEMENTATION
// ----------------------------------------

#if THREADMODEL==WINRTTHREADS

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::System::Threading;

static void start(GThread *gt)
{
    try
    {
        G_TRY
        {
            gt->xentry(gt->xarg);
        }
            G_CATCH(ex)
        {
            ex.perror();
            DjVuMessageLite::perror(ERR_MSG("GThreads.uncaught"));
#ifdef _DEBUG
            abort();
#endif
        }
        G_ENDCATCH;
    }
    catch (...)
    {
        DjVuMessageLite::perror(ERR_MSG("GThreads.unrecognized"));
#ifdef _DEBUG
        abort();
#endif
    }
}

GThread::GThread(int stacksize)
    : xentry(0), xarg(0)
{
}

GThread::~GThread()
{
}

int
GThread::create(void(*entry)(void*), void *arg)
{
    xentry = entry;
    xarg = arg;

    auto workItemHandler = ref new WorkItemHandler([=](IAsyncAction^)
    {
        start(this);
    });

    ThreadPool::RunAsync(workItemHandler);

    return 0;
}

void
GThread::terminate()
{
    OutputDebugString(TEXT("Terminating thread.\n"));
    throw ref new Platform::NotImplementedException();
}

int
GThread::yield()
{
    Sleep(0);
    return 0;
}

void *
GThread::current()
{
    return (void*)GetCurrentThreadId();
}

struct thr_waiting {
    struct thr_waiting *next;
    struct thr_waiting *prev;
    BOOL   waiting;
    HANDLE gwait;
};

GMonitor::GMonitor()
    : ok(0), count(1), head(0), tail(0)
{
    InitializeCriticalSectionEx(&cs, 4000, 0);
    locker = GetCurrentThreadId();
    ok = 1;
}

GMonitor::~GMonitor()
{
    ok = 0;
    EnterCriticalSection(&cs);
    for (struct thr_waiting *w = head; w; w = w->next)
        SetEvent(w->gwait);
    LeaveCriticalSection(&cs);
    DeleteCriticalSection(&cs);
}

void
GMonitor::enter()
{
    DWORD self = GetCurrentThreadId();
    if (count>0 || self != locker)
    {
        if (ok)
            EnterCriticalSection(&cs);
        locker = self;
        count = 1;
    }
    count -= 1;
}

void
GMonitor::leave()
{
    DWORD self = GetCurrentThreadId();
    if (ok && (count>0 || self != locker))
        G_THROW(ERR_MSG("GThreads.not_acq_broad"));
    count += 1;
    if (count > 0)
    {
        count = 1;
        if (ok)
            LeaveCriticalSection(&cs);
    }
}

void
GMonitor::signal()
{
    if (ok)
    {
        DWORD self = GetCurrentThreadId();
        if (count>0 || self != locker)
            G_THROW(ERR_MSG("GThreads.not_acq_signal"));
        for (struct thr_waiting *w = head; w; w = w->next)
            if (w->waiting)
            {
                SetEvent(w->gwait);
                w->waiting = FALSE;
                break; // Only one thread is allowed to run!
            }
    }
}

void
GMonitor::broadcast()
{
    if (ok)
    {
        DWORD self = GetCurrentThreadId();
        if (count>0 || self != locker)
            G_THROW(ERR_MSG("GThreads.not_acq_broad"));
        for (struct thr_waiting *w = head; w; w = w->next)
            if (w->waiting)
            {
                SetEvent(w->gwait);
                w->waiting = FALSE;
            }
    }
}

void
GMonitor::wait()
{
    // Check state
    DWORD self = GetCurrentThreadId();
    if (count>0 || self != locker)
        G_THROW(ERR_MSG("GThreads.not_acq_wait"));
    // Wait
    if (ok)
    {
        // Prepare wait record
        struct thr_waiting waitrec;
        waitrec.waiting = TRUE;
        waitrec.gwait = CreateEventEx(nullptr, nullptr, 0, EVENT_ALL_ACCESS);
        waitrec.next = 0;
        waitrec.prev = tail;
        // Link wait record (protected by critical section)
        *(waitrec.next ? &waitrec.next->prev : &tail) = &waitrec;
        *(waitrec.prev ? &waitrec.prev->next : &head) = &waitrec;
        // Start wait
        int sav_count = count;
        count = 1;
        LeaveCriticalSection(&cs);
        WaitForSingleObjectEx(waitrec.gwait, INFINITE, false);
        // Re-acquire
        EnterCriticalSection(&cs);
        count = sav_count;
        locker = self;
        // Unlink wait record
        *(waitrec.next ? &waitrec.next->prev : &tail) = waitrec.prev;
        *(waitrec.prev ? &waitrec.prev->next : &head) = waitrec.next;
        CloseHandle(waitrec.gwait);
    }
}

void
GMonitor::wait(unsigned long timeout)
{
    // Check state
    DWORD self = GetCurrentThreadId();
    if (count>0 || self != locker)
        G_THROW(ERR_MSG("GThreads.not_acq_wait"));
    // Wait
    if (ok)
    {
        // Prepare wait record
        struct thr_waiting waitrec;
        waitrec.waiting = TRUE;
        waitrec.gwait = CreateEventEx(nullptr, nullptr, 0, EVENT_ALL_ACCESS);
        waitrec.next = 0;
        waitrec.prev = tail;
        // Link wait record (protected by critical section)
        *(waitrec.prev ? &waitrec.prev->next : &head) = &waitrec;
        *(waitrec.next ? &waitrec.next->prev : &tail) = &waitrec;
        // Start wait
        int sav_count = count;
        count = 1;
        LeaveCriticalSection(&cs);
        WaitForSingleObjectEx(waitrec.gwait, timeout, false);
        // Re-acquire
        EnterCriticalSection(&cs);
        count = sav_count;
        locker = self;
        // Unlink wait record
        *(waitrec.next ? &waitrec.next->prev : &tail) = waitrec.prev;
        *(waitrec.prev ? &waitrec.prev->next : &head) = waitrec.next;
        CloseHandle(waitrec.gwait);
    }
}

#endif

#ifdef HAVE_NAMESPACES
}
# ifndef NOT_USING_DJVU_NAMESPACE
using namespace DJVU;
# endif
#endif
