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
# pragma implementation "ddjvuapi.h"
#endif

#include <stddef.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <locale.h>

#ifdef HAVE_NAMESPACES
namespace DJVU {
  struct ddjvu_context_s;
  struct ddjvu_job_s;
  struct ddjvu_document_s;
  struct ddjvu_page_s;
  struct ddjvu_format_s;
  struct ddjvu_message_p;
  struct ddjvu_thumbnail_p;
  struct ddjvu_runnablejob_s;
  struct ddjvu_printjob_s;
  struct ddjvu_savejob_s;
}
using namespace DJVU;
# define DJVUNS DJVU::
#else
# define DJVUNS /**/
#endif

#include "GException.h"
#include "GSmartPointer.h"
#include "GThreads.h"
#include "GContainer.h"
#include "ByteStream.h"
#include "IFFByteStream.h"
#include "BSByteStream.h"
#include "GString.h"
#include "GBitmap.h"
#include "GPixmap.h"
#include "GScaler.h"
#include "DjVuPort.h"
#include "DataPool.h"
#include "DjVuInfo.h"
#include "IW44Image.h"
#include "DjVuImage.h"
#include "DjVuFileCache.h"
#include "DjVuDocument.h"
//#include "DjVuDumpHelper.h"
#include "DjVuMessageLite.h"
//EDITED
//#include "DjVuMessage.h"
#include "DjVmNav.h"
#include "DjVuText.h"
#include "DjVuAnno.h"
//#include "DjVuToPS.h"
#include "DjVmDir.h"
#include "DjVmDir0.h"
#include "DjVuNavDir.h"
#include "DjVmDoc.h"


#include "miniexp.h"
#include "ddjvuapi.h"


// ----------------------------------------
// Private structures


struct DJVUNS ddjvu_message_p : public GPEnabled
{
  GNativeString tmp1;
  GNativeString tmp2;
  ddjvu_message_t p;
  ddjvu_message_p() { memset(&p, 0, sizeof(p)); }
};

struct DJVUNS ddjvu_thumbnail_p : public GPEnabled
{
  ddjvu_document_t *document;
  int pagenum;
  GTArray<char> data;
  GP<DataPool> pool;
  static void callback(void *);
}; 


// ----------------------------------------
// Context, Jobs, Document, Pages


struct DJVUNS ddjvu_context_s : public GPEnabled
{
  GMonitor monitor;
  GP<DjVuFileCache> cache;
  GPList<ddjvu_message_p> mlist;
  GP<ddjvu_message_p> mpeeked;
  int uniqueid;
  ddjvu_message_callback_t callbackfun;
  void *callbackarg;
};

struct DJVUNS ddjvu_job_s : public DjVuPort
{
  GMonitor monitor;
  void *userdata;
  GP<ddjvu_context_s> myctx;
  GP<ddjvu_document_s> mydoc;
  bool released;
  ddjvu_job_s();
  // virtual port functions:
  virtual bool inherits(const GUTF8String&);
  virtual bool notify_error(const DjVuPort*, const GUTF8String&);  
  virtual bool notify_status(const DjVuPort*, const GUTF8String&);
  // default implementation of virtual job functions:
  virtual ddjvu_status_t status() {return DDJVU_JOB_NOTSTARTED;}
  virtual void release() {}
  virtual void stop() {}
};

struct DJVUNS ddjvu_document_s : public ddjvu_job_s
{
  GP<DjVuDocument> doc;
  GPMap<int,DataPool> streams;
  GMap<GUTF8String, int> names;
  GPMap<int,ddjvu_thumbnail_p> thumbnails;
  int streamid;
  bool fileflag;
  bool urlflag;
  bool docinfoflag;
  bool pageinfoflag;
  //minivar_t protect;
  // virtual job functions:
  virtual ddjvu_status_t status();
  virtual void release();
  // virtual port functions:
  virtual bool inherits(const GUTF8String&);
  virtual bool notify_error(const DjVuPort*, const GUTF8String&);  
  virtual bool notify_status(const DjVuPort*, const GUTF8String&);
  virtual void notify_doc_flags_changed(const DjVuDocument*, long, long);
  virtual GP<DataPool> request_data(const DjVuPort*, const GURL&);
  static void callback(void *);
  bool want_pageinfo(void);
};

struct DJVUNS ddjvu_page_s : public ddjvu_job_s
{
  GP<DjVuImage> img;
  ddjvu_job_t *job;
  bool pageinfoflag;            // was the first m_pageinfo sent?
  bool pagedoneflag;            // was the final m_pageinfo sent?
  // virtual job functions:
  virtual ddjvu_status_t status();
  virtual void release();
  // virtual port functions:
  virtual bool inherits(const GUTF8String&);
  virtual bool notify_error(const DjVuPort*, const GUTF8String&);  
  virtual bool notify_status(const DjVuPort*, const GUTF8String&);
  virtual void notify_file_flags_changed(const DjVuFile*, long, long);
  virtual void notify_relayout(const class DjVuImage*);
  virtual void notify_redisplay(const class DjVuImage*);
  virtual void notify_chunk_done(const DjVuPort*, const GUTF8String &);
  void sendmessages();
};




// ----------------------------------------
// Helpers


// Hack to increment counter
static void 
ref(GPEnabled *p)
{
  GPBase n(p);
  char *gn = (char*)&n;
  *(GPEnabled**)gn = 0;
  n.assign(0);
}

// Hack to decrement counter
static void 
unref(GPEnabled *p)
{
  GPBase n;
  char *gn = (char*)&n;
  *(GPEnabled**)gn = p;
  n.assign(0);
}

// Allocate strings
static char *
xstr(const char *s)
{
  int l = strlen(s);
  char *p = (char*)malloc(l + 1);
  if (p) 
    {
      strcpy(p, s);
      p[l] = 0;
    }
  return p;
}

// Allocate strings
static char *
xstr(const GNativeString &n)
{
  return xstr( (const char*) n );
}

// Allocate strings
static char *
xstr(const GUTF8String &u)
{
  GNativeString n(u);
  return xstr( n );
}

// Fill a message head
static ddjvu_message_any_t 
xhead(ddjvu_message_tag_t tag,
      ddjvu_context_t *context)
{
  ddjvu_message_any_t any;
  any.tag = tag;
  any.context = context;
  any.document = 0;
  any.page = 0;
  any.job = 0;
  return any;
}
static ddjvu_message_any_t 
xhead(ddjvu_message_tag_t tag,
      ddjvu_job_t *job)
{
  ddjvu_message_any_t any;
  any.tag = tag;
  any.context = job->myctx;
  any.document = job->mydoc;
  any.page = 0;
  any.job = job;
  return any;
}
static ddjvu_message_any_t 
xhead(ddjvu_message_tag_t tag,
      ddjvu_document_t *document)
{
  ddjvu_message_any_t any;
  any.tag = tag;
  any.context = document->myctx;
  any.document = document;
  any.page = 0;
  any.job = document;
  return any;
}
static ddjvu_message_any_t 
xhead(ddjvu_message_tag_t tag,
      ddjvu_page_t *page)
{
  ddjvu_message_any_t any;
  any.tag = tag;
  any.context = page->myctx;
  any.document = page->mydoc;
  any.page = page;
  any.job = page->job;
  return any;
}


// ----------------------------------------
// Version

const char*
ddjvu_get_version_string(void)
{
#ifdef DJVULIBRE_VERSION
  return "DjVuLibre-" DJVULIBRE_VERSION;
#else
  return "DjVuLibre";
#endif
}

int
ddjvu_code_get_version(void)
{
  return DJVUVERSION;
}




// ----------------------------------------
// Context


ddjvu_context_t *
ddjvu_context_create(const char *programname)
{
  ddjvu_context_t *ctx = 0;
  G_TRY
    {
#ifdef LC_ALL
      setlocale(LC_ALL,"");
# ifdef LC_NUMERIC
      setlocale(LC_NUMERIC, "C");
# endif
#endif
      //if (programname)
      //  djvu_programname(programname);
      //EDITED
	  //DjVuMessage::use_language();
      DjVuMessageLite::create();
      ctx = new ddjvu_context_s;
      ref(ctx);
      ctx->uniqueid = 0;
      ctx->callbackfun = 0;
      ctx->callbackarg = 0;
      ctx->cache = DjVuFileCache::create();
    }
  G_CATCH_ALL
    {
      if (ctx)
        unref(ctx);
      ctx = 0;
    }
  G_ENDCATCH;
  return ctx;
}

void 
ddjvu_context_release(ddjvu_context_t *ctx)
{
  G_TRY
    {
      if (ctx)
        unref(ctx);
    }
  G_CATCH_ALL
    {
    }
  G_ENDCATCH;
}


// ----------------------------------------
// Message helpers


// post a new message
static void
msg_push(const ddjvu_message_any_t &head,
         GP<ddjvu_message_p> msg = 0)
{
  ddjvu_context_t *ctx = head.context;
  if (! msg) 
    msg = new ddjvu_message_p;
  msg->p.m_any = head; 
  GMonitorLock lock(&ctx->monitor);
  if ((head.document && head.document->released) ||
      (head.page && head.page->released) ||
      (head.job && head.job->released) )
    return;
  if (ctx->callbackfun) 
    (*ctx->callbackfun)(ctx, ctx->callbackarg);
  ctx->mlist.append(msg);
  ctx->monitor.broadcast();
}

static void
msg_push_nothrow(const ddjvu_message_any_t &head,
                 GP<ddjvu_message_p> msg = 0)
{
  G_TRY
    {
      msg_push(head, msg);
    }
  G_CATCH_ALL
    {
    }
  G_ENDCATCH;
}

// prepare error message from string
static GP<ddjvu_message_p>
msg_prep_error(GUTF8String message,
               const char *function=0, 
               const char *filename=0, 
               int lineno=0)
{
  GP<ddjvu_message_p> p = new ddjvu_message_p;
  p->p.m_error.message = 0;
  p->p.m_error.function = function;
  p->p.m_error.filename = filename;
  p->p.m_error.lineno = lineno;
  G_TRY 
    { 
      p->tmp1 = DjVuMessageLite::LookUpUTF8(message);
      p->p.m_error.message = (const char*)(p->tmp1);
    }
  G_CATCH_ALL 
    {
    } 
  G_ENDCATCH;
  return p;
}

// prepare error message from exception
static GP<ddjvu_message_p>
msg_prep_error(const GException &ex,
               const char *function=0, 
               const char *filename=0, 
               int lineno=0)
{
  GP<ddjvu_message_p> p = new ddjvu_message_p;
  p->p.m_error.message = 0;
  p->p.m_error.function = function;
  p->p.m_error.filename = filename;
  p->p.m_error.lineno = lineno;
  G_TRY 
    { 
      p->tmp1 = DjVuMessageLite::LookUpUTF8(ex.get_cause());
      p->p.m_error.message = (const char*)(p->tmp1);
      p->p.m_error.function = ex.get_function();
      p->p.m_error.filename = ex.get_file();
      p->p.m_error.lineno = ex.get_line();
    }
  G_CATCH_ALL 
    {
    } 
  G_ENDCATCH;
  return p;
}

// prepare status message
static GP<ddjvu_message_p>
msg_prep_info(GUTF8String message)
{
  GP<ddjvu_message_p> p = new ddjvu_message_p;
  p->tmp1 = DjVuMessageLite::LookUpUTF8(message); // i18n nonsense!
  p->p.m_info.message = (const char*)(p->tmp1);
  return p;
}

// ----------------------------------------


#ifdef __GNUG__
# define ERROR1(x, m) \
    msg_push_nothrow(xhead(DDJVU_ERROR,x),\
                     msg_prep_error(m,__func__,__FILE__,__LINE__))
#else
# define ERROR1(x, m) \
    msg_push_nothrow(xhead(DDJVU_ERROR,x),\
                     msg_prep_error(m,0,__FILE__,__LINE__))
#endif


// ----------------------------------------
// Cache

void
ddjvu_cache_set_size(ddjvu_context_t *ctx,
                     unsigned long cachesize)
{
  G_TRY
    {
      GMonitorLock lock(&ctx->monitor);
      if (ctx->cache && cachesize>0)
        ctx->cache->set_max_size(cachesize);
    }
  G_CATCH(ex) 
    {
      ERROR1(ctx, ex);
    }
  G_ENDCATCH;
}

DDJVUAPI unsigned long
ddjvu_cache_get_size(ddjvu_context_t *ctx)
{
  G_TRY
    {
      GMonitorLock lock(&ctx->monitor);
      if (ctx->cache)
        return ctx->cache->get_max_size();
    }
  G_CATCH(ex) 
    { 
      ERROR1(ctx, ex);
    }
  G_ENDCATCH;
  return 0;
}

void
ddjvu_cache_clear(ddjvu_context_t *ctx)
{
  G_TRY
    {
      GMonitorLock lock(&ctx->monitor);
      DataPool::close_all();
      if (ctx->cache)
      {
        ctx->cache->clear();
        return;
      }
    }
  G_CATCH(ex)
    {
      ERROR1(ctx, ex);
    }
  G_ENDCATCH;
 }


// ----------------------------------------
// Jobs

ddjvu_job_s::ddjvu_job_s()
  : userdata(0), released(false)
{
}

bool
ddjvu_job_s::inherits(const GUTF8String &classname)
{
  return (classname == "ddjvu_job_s") 
    || DjVuPort::inherits(classname);
}

bool 
ddjvu_job_s::notify_error(const DjVuPort *, const GUTF8String &m)
{
  msg_push(xhead(DDJVU_ERROR, this), msg_prep_error(m));
  return true;
}

bool 
ddjvu_job_s::notify_status(const DjVuPort *p, const GUTF8String &m)
{
  msg_push(xhead(DDJVU_INFO, this), msg_prep_info(m));
  return true;
}

void
ddjvu_job_release(ddjvu_job_t *job)
{
  G_TRY
    {
      if (!job)
        return;
      job->release();
      job->userdata = 0;
      job->released = true;
      // clean all messages
      ddjvu_context_t *ctx = job->myctx;
      if (ctx)
        {
          GMonitorLock lock(&ctx->monitor);
          GPosition p = ctx->mlist;
          while (p) 
            {
              GPosition s = p; ++p;
              if (ctx->mlist[s]->p.m_any.job == job ||
                  ctx->mlist[s]->p.m_any.document == job ||
                  ctx->mlist[s]->p.m_any.page == job )
                ctx->mlist.del(s);
            }
          // cleanup pointers in current message as well.
          if (ctx->mpeeked)
            {
              ddjvu_message_t *m = &ctx->mpeeked->p;
              if (m->m_any.job == job)       
                m->m_any.job = 0;
              if (m->m_any.document == job)
                m->m_any.document = 0;
              if (m->m_any.page == job)
                m->m_any.page = 0;
            }
        }
      // decrement reference counter
      unref(job);
    }
  G_CATCH_ALL
    {
    }
  G_ENDCATCH;
}

ddjvu_status_t
ddjvu_job_status(ddjvu_job_t *job)
{
  G_TRY
    {
      if (! job)
        return DDJVU_JOB_NOTSTARTED;
      return job->status();
    }
  G_CATCH(ex)
    {
      ERROR1(job, ex);
    }
  G_ENDCATCH;
  return DDJVU_JOB_FAILED;
}

void
ddjvu_job_stop(ddjvu_job_t *job)
{
  G_TRY
    {
      if (job)
        job->stop();
    }
  G_CATCH(ex)
    {
      ERROR1(job, ex);
    }
  G_ENDCATCH;
}

void
ddjvu_job_set_user_data(ddjvu_job_t *job, void *userdata)
{
  if (job)
    job->userdata = userdata;
}

void *
ddjvu_job_get_user_data(ddjvu_job_t *job)
{
  if (job)
    return job->userdata;
  return 0;
}


// ----------------------------------------
// Message queue


ddjvu_message_t *
ddjvu_message_peek(ddjvu_context_t *ctx)
{
  G_TRY
    {
      GMonitorLock lock(&ctx->monitor);
      if (ctx->mpeeked)
        return &ctx->mpeeked->p;        
      if (! ctx->mlist.size())
        ctx->monitor.wait(0);
      GPosition p = ctx->mlist;
      if (! p)
        return 0;
      ctx->mpeeked = ctx->mlist[p];
      ctx->mlist.del(p);
      return &ctx->mpeeked->p;
    }
  G_CATCH_ALL
    {
    }
  G_ENDCATCH;
  return 0;
}

ddjvu_message_t *
ddjvu_message_wait(ddjvu_context_t *ctx)
{
  G_TRY
    {
      GMonitorLock lock(&ctx->monitor);
      if (ctx->mpeeked)
        return &ctx->mpeeked->p;        
      while (! ctx->mlist.size())
        ctx->monitor.wait();
      GPosition p = ctx->mlist;
      if (! p)
        return 0;
      ctx->mpeeked = ctx->mlist[p];
      ctx->mlist.del(p);
      return &ctx->mpeeked->p;        
    }
  G_CATCH_ALL
    {
    }
  G_ENDCATCH;
  return 0;
}

void
ddjvu_message_pop(ddjvu_context_t *ctx)
{
  G_TRY
    {
      GMonitorLock lock(&ctx->monitor);
      ctx->mpeeked = 0;
    }
  G_CATCH_ALL
    {
    }
  G_ENDCATCH;
}

void
ddjvu_message_set_callback(ddjvu_context_t *ctx,
                           ddjvu_message_callback_t callback,
                           void *closure)
{
  GMonitorLock lock(&ctx->monitor);
  ctx->callbackfun = callback;
  ctx->callbackarg = closure;
}


// ----------------------------------------
// Document callbacks


void
ddjvu_document_s::release()
{
  GPosition p;
  GMonitorLock lock(&monitor);
  doc = 0;
  for (p=thumbnails; p; ++p)
    {
      ddjvu_thumbnail_p *thumb = thumbnails[p];
      if (thumb->pool)
        thumb->pool->del_trigger(ddjvu_thumbnail_p::callback, (void*)thumb);
    }
  for (p = streams; p; ++p)
    {
      GP<DataPool> pool = streams[p];
      if (pool)
        pool->del_trigger(callback, (void*)this);
      if (pool && !pool->is_eof())
        pool->stop();
    }
}

ddjvu_status_t
ddjvu_document_s::status()
{
  if (!doc)
    return DDJVU_JOB_NOTSTARTED;
  long flags = doc->get_doc_flags();
  if (flags & DjVuDocument::DOC_INIT_OK)
    return DDJVU_JOB_OK;
  else if (flags & DjVuDocument::DOC_INIT_FAILED)
    return DDJVU_JOB_FAILED;
  return DDJVU_JOB_STARTED;
}

bool
ddjvu_document_s::inherits(const GUTF8String &classname)
{
  return (classname == "ddjvu_document_s")
    || ddjvu_job_s::inherits(classname);
}

bool 
ddjvu_document_s::notify_error(const DjVuPort *, const GUTF8String &m)
{
  if (!doc) return false;
  msg_push(xhead(DDJVU_ERROR, this), msg_prep_error(m));
  return true;
}
 
bool 
ddjvu_document_s::notify_status(const DjVuPort *p, const GUTF8String &m)
{
  if (!doc) return false;
  msg_push(xhead(DDJVU_INFO, this), msg_prep_info(m));
  return true;
}

void 
ddjvu_document_s::notify_doc_flags_changed(const DjVuDocument *, long, long)
{
  GMonitorLock lock(&monitor);
  if (docinfoflag || !doc) return;
  long flags = doc->get_doc_flags();
  if ((flags & DjVuDocument::DOC_INIT_OK) ||
      (flags & DjVuDocument::DOC_INIT_FAILED) )
  {
    msg_push(xhead(DDJVU_DOCINFO, this));
    docinfoflag = true;
  }
}


void 
ddjvu_document_s::callback(void *arg)
{
  ddjvu_document_t *doc = (ddjvu_document_t *)arg;
  if (doc && doc->pageinfoflag && !doc->fileflag) 
    msg_push(xhead(DDJVU_PAGEINFO, doc));
}


GP<DataPool> 
ddjvu_document_s::request_data(const DjVuPort *p, const GURL &url)
{
  // Note: the following line try to restore
  //       the bytes stored in the djvu file
  //       despite LT's i18n and gurl classes.
  GUTF8String name = (const char*)url.fname(); 
  GMonitorLock lock(&monitor);
  GP<DataPool> pool;
  if (names.contains(name))
    {
      int streamid = names[name];
      return streams[streamid];
    }
  else if (fileflag)
    {
      if (doc && url.is_local_file_url())
        return DataPool::create(url);
    }
  else if (doc)
    {
      // prepare pool
      if (++streamid > 0)
        streams[streamid] = pool = DataPool::create();
      else
        pool = streams[(streamid = 0)];
      names[name] = streamid;
      pool->add_trigger(-1, callback, (void*)this);
      // build message
      GP<ddjvu_message_p> p = new ddjvu_message_p;
      p->p.m_newstream.streamid = streamid;
      p->tmp1 = name;
      p->p.m_newstream.name = (const char*)(p->tmp1);
      p->p.m_newstream.url = 0;
      if (urlflag)
        {
          // Should be urlencoded.
          p->tmp2 = (const char*)url.get_string();
          p->p.m_newstream.url = (const char*)(p->tmp2);
        }
      msg_push(xhead(DDJVU_NEWSTREAM, this), p);
    }
  return pool;
}


bool
ddjvu_document_s::want_pageinfo()
{
  if (doc && docinfoflag && !pageinfoflag)
    {
      pageinfoflag = true;
      int doctype = doc->get_doc_type();
      if (doctype == DjVuDocument::BUNDLED ||
          doctype == DjVuDocument::OLD_BUNDLED )
        {
          GP<DataPool> pool;
          {
            GMonitorLock lock(&monitor);
            if (streams.contains(0))
              pool = streams[0];
          }
          if (pool && doctype == DjVuDocument::BUNDLED)
            {
              GP<DjVmDir> dir = doc->get_djvm_dir();
              if (dir)
                for (int i=0; i<dir->get_files_num(); i++)
                  {
                    GP<DjVmDir::File> f = dir->pos_to_file(i);
                    if (! pool->has_data(f->offset, f->size))
                      pool->add_trigger(f->offset, f->size, callback, (void*)this );
                  }
            }
          else if (pool && doctype == DjVuDocument::OLD_BUNDLED)
            {
              GP<DjVmDir0> dir = doc->get_djvm_dir0();
              if (dir)
                for (int i=0; i<dir->get_files_num(); i++)
                  {
                    GP<DjVmDir0::FileRec> f = dir->get_file(i);
                    if (! pool->has_data(f->offset, f->size))
                      pool->add_trigger(f->offset, f->size, callback, (void*)this );
                  }
            }
        }
    }
  return pageinfoflag;
}


// ----------------------------------------
// Documents


ddjvu_document_t *
ddjvu_document_create(ddjvu_context_t *ctx,
                      const char *url,
                      int cache)
{
  ddjvu_document_t *d = 0;
  G_TRY
    {
      DjVuFileCache *xcache = ctx->cache;
      if (! cache) xcache = 0;
      d = new ddjvu_document_s;
      ref(d);
      GMonitorLock lock(&d->monitor);
      d->streams[0] = DataPool::create();
      d->streamid = -1;
      d->fileflag = false;
      d->docinfoflag = false;
      d->pageinfoflag = false;
      d->myctx = ctx;
      d->mydoc = 0;
      d->doc = DjVuDocument::create_noinit();
      if (url)
        {
          GURL gurl = GUTF8String(url);
          gurl.clear_djvu_cgi_arguments();
          d->urlflag = true;
          d->doc->start_init(gurl, d, xcache);
        }
      else
        {
          GUTF8String s;
          s.format("ddjvu:///doc%d/index.djvu", ++(ctx->uniqueid));;
          GURL gurl = s;
          d->urlflag = false;
          d->doc->start_init(gurl, d, xcache);
        }
    }
  G_CATCH(ex)
    {
      if (d) 
        unref(d);
      d = 0;
      ERROR1(ctx, ex);
    }
  G_ENDCATCH;
  return d;
}

static ddjvu_document_t *
ddjvu_document_create_by_filename_imp(ddjvu_context_t *ctx,
                                      const char *filename,
                                      int cache, int utf8)
{
  ddjvu_document_t *d = 0;
  G_TRY
    {
      DjVuFileCache *xcache = ctx->cache;
      if (! cache) xcache = 0;
      GURL gurl;
      if (utf8) 
        gurl = GURL::Filename::UTF8(filename);
      else
        gurl = GURL::Filename::Native(filename);
      d = new ddjvu_document_s;
      ref(d);
      GMonitorLock lock(&d->monitor);
      d->streamid = -1;
      d->fileflag = true;
      d->pageinfoflag = false;
      d->urlflag = false;
      d->docinfoflag = false;
      d->myctx = ctx;
      d->mydoc = 0;
      d->doc = DjVuDocument::create_noinit();
      d->doc->start_init(gurl, d, xcache);
    }
  G_CATCH(ex)
    {
      if (d)
        unref(d);
      d = 0;
      ERROR1(ctx, ex);
    }
  G_ENDCATCH;
  return d;
}

ddjvu_document_t *
ddjvu_document_create_by_file_struct(ddjvu_context_t *ctx,
                                      FILE* file,
                                      int cache)
{
  ddjvu_document_t *d = 0;
  G_TRY
    {
      DjVuFileCache *xcache = ctx->cache;
      if (! cache) xcache = 0;
      d = new ddjvu_document_s;
      ref(d);
      GMonitorLock lock(&d->monitor);
      d->streamid = -1;
      d->fileflag = true;
      d->pageinfoflag = false;
      d->urlflag = false;
      d->docinfoflag = false;
      d->myctx = ctx;
      d->mydoc = 0;
	  GP<ByteStream> stream = ByteStream::create(file, "rb", true);
	  GP<DjVuDocument> doc = DjVuDocument::create(stream);
      d->doc = doc;
    }
  G_CATCH(ex)
    {
      if (d)
        unref(d);
      d = 0;
      ERROR1(ctx, ex);
    }
  G_ENDCATCH;
  return d;
}

ddjvu_document_t *
ddjvu_document_create_by_filename(ddjvu_context_t *ctx,
                                  const char *filename,
                                  int cache)
{
  return ddjvu_document_create_by_filename_imp(ctx,filename,cache,0);
}

ddjvu_document_t *
ddjvu_document_create_by_filename_utf8(ddjvu_context_t *ctx,
                                       const char *filename,
                                       int cache)
{
  return ddjvu_document_create_by_filename_imp(ctx,filename,cache,1);
}

ddjvu_job_t *
ddjvu_document_job(ddjvu_document_t *document)
{
  return document;
}


// ----------------------------------------
// Streams


void
ddjvu_stream_write(ddjvu_document_t *doc,
                   int streamid,
                   const char *data,
                   unsigned long datalen )
{
  G_TRY
    {
      GP<DataPool> pool;
      { 
        GMonitorLock lock(&doc->monitor); 
        GPosition p = doc->streams.contains(streamid);
        if (p) pool = doc->streams[p];
      }
      if (! pool)
        G_THROW("Unknown stream ID");
      if (datalen > 0)
        pool->add_data(data, datalen);
    }
  G_CATCH(ex)
    {
      ERROR1(doc,ex);
    }
  G_ENDCATCH;
}

void
ddjvu_stream_close(ddjvu_document_t *doc,
                   int streamid,
                   int stop )
{
  G_TRY
    {
      GP<DataPool> pool;
      { 
        GMonitorLock lock(&doc->monitor); 
        GPosition p = doc->streams.contains(streamid);
        if (p) pool = doc->streams[p];
      }
      if (! pool)
        G_THROW("Unknown stream ID");
      if (stop)
        pool->stop(true);
      pool->set_eof();
    }
  G_CATCH(ex)
    {
      ERROR1(doc, ex);
    }
  G_ENDCATCH;
}


// ----------------------------------------
// Document queries


ddjvu_document_type_t
ddjvu_document_get_type(ddjvu_document_t *document)
{
  G_TRY
    {
      DjVuDocument *doc = document->doc;
      if (doc)
        {
          switch (doc->get_doc_type())
            {
            case DjVuDocument::OLD_BUNDLED:
              return DDJVU_DOCTYPE_OLD_BUNDLED;
            case DjVuDocument::OLD_INDEXED:
              return DDJVU_DOCTYPE_OLD_INDEXED;
            case DjVuDocument::BUNDLED:
              return DDJVU_DOCTYPE_BUNDLED;
            case DjVuDocument::INDIRECT:
              return DDJVU_DOCTYPE_INDIRECT;
            case DjVuDocument::SINGLE_PAGE:
              return DDJVU_DOCTYPE_SINGLEPAGE;
            default:
              break;
            }
        }
    }
  G_CATCH(ex)
    {
      ERROR1(document,ex);
    }
  G_ENDCATCH;
  return DDJVU_DOCTYPE_UNKNOWN;
}


int
ddjvu_document_get_pagenum(ddjvu_document_t *document)
{
  G_TRY
    {
      DjVuDocument *doc = document->doc;
      if (doc)
        return doc->get_pages_num();
    }
  G_CATCH(ex)
    {
      ERROR1(document,ex);
    }
  G_ENDCATCH;
  return 1;
}


int
ddjvu_document_get_filenum(ddjvu_document_t *document)
{
  G_TRY
    {
      DjVuDocument *doc = document->doc;
      if (! (doc && doc->is_init_ok()))
        return 0;
      int doc_type = doc->get_doc_type();
      if (doc_type == DjVuDocument::BUNDLED ||
          doc_type == DjVuDocument::INDIRECT )
        {
          GP<DjVmDir> dir = doc->get_djvm_dir();
          return dir->get_files_num();
        }
      else if (doc_type == DjVuDocument::OLD_BUNDLED)
        {
          GP<DjVmDir0> dir0 = doc->get_djvm_dir0();
          return dir0->get_files_num();
        }
      else 
        return doc->get_pages_num();
    }
  G_CATCH(ex)
    {
      ERROR1(document,ex);
    }
  G_ENDCATCH;
  return 0;
}


#undef ddjvu_document_get_fileinfo

extern "C" DDJVUAPI ddjvu_status_t
ddjvu_document_get_fileinfo(ddjvu_document_t *d, int f, ddjvu_fileinfo_t *i);

ddjvu_status_t
ddjvu_document_get_fileinfo(ddjvu_document_t *d, int f, ddjvu_fileinfo_t *i)
{
  // for binary backward compatibility with ddjvuapi=17
  struct info17_s { char t; int p,s; const char *d, *n, *l; };
  return ddjvu_document_get_fileinfo_imp(d,f,i,sizeof(info17_s));
}

ddjvu_status_t
ddjvu_document_get_fileinfo_imp(ddjvu_document_t *document, int fileno, 
                                ddjvu_fileinfo_t *info, 
                                unsigned int infosz )
{
  G_TRY
    {
      ddjvu_fileinfo_t myinfo;
      memset(info, 0, infosz);
      if (infosz > sizeof(myinfo))
        return DDJVU_JOB_FAILED;
      DjVuDocument *doc = document->doc;
      if (! doc)
        return DDJVU_JOB_NOTSTARTED;
      if (! doc->is_init_ok())
        return document->status();
      int type = doc->get_doc_type();
      if ( type == DjVuDocument::BUNDLED ||
           type == DjVuDocument::INDIRECT )
        {
          GP<DjVmDir> dir = doc->get_djvm_dir();
          GP<DjVmDir::File> file = dir->pos_to_file(fileno, &myinfo.pageno);
          if (! file)
            G_THROW("Illegal file number");
          myinfo.type = 'I';
          if (file->is_page())
            myinfo.type = 'P';
          else
            myinfo.pageno = -1;
          if (file->is_thumbnails())
            myinfo.type = 'T';
          if (file->is_shared_anno())
            myinfo.type = 'S';
          myinfo.size = file->size;
          myinfo.id = file->get_load_name();
          myinfo.name = file->get_save_name();
          myinfo.title = file->get_title();
          memcpy(info, &myinfo, infosz);
          return DDJVU_JOB_OK;
        }
      else if (type == DjVuDocument::OLD_BUNDLED)
        {
          GP<DjVmDir0> dir0 = doc->get_djvm_dir0();
          GP<DjVuNavDir> nav = doc->get_nav_dir();
          GP<DjVmDir0::FileRec> frec = dir0->get_file(fileno);
          if (! frec)
            G_THROW("Illegal file number");
          myinfo.size = frec->size;
          myinfo.id = (const char*) frec->name;
          myinfo.name = myinfo.title = myinfo.id;
          if (! nav)
            return DDJVU_JOB_STARTED;
          else if (nav->name_to_page(frec->name) >= 0)
            myinfo.type = 'P';
          else
            myinfo.type = 'I';
          memcpy(info, &myinfo, infosz);
          return DDJVU_JOB_OK;
        }
      else 
        {
          if (fileno<0 || fileno>=doc->get_pages_num())
            G_THROW("Illegal file number");
          myinfo.type = 'P';
          myinfo.pageno = fileno;
          myinfo.size = -1;
          GP<DjVuNavDir> nav = doc->get_nav_dir();
          myinfo.id = (nav) ? (const char *) nav->page_to_name(fileno) : 0;
          myinfo.name = myinfo.title = myinfo.id;
          GP<DjVuFile> file = doc->get_djvu_file(fileno, true);
          GP<DataPool> pool; 
          if (file) 
            pool = file->get_init_data_pool();
          if (pool)
            myinfo.size = pool->get_length();
          memcpy(info, &myinfo, infosz);
          return DDJVU_JOB_OK;
        }
    }
  G_CATCH(ex)
    {
      ERROR1(document,ex);
    }
  G_ENDCATCH;
  return DDJVU_JOB_FAILED;
}


int
ddjvu_document_search_pageno(ddjvu_document_t *document, const char *name)
{
  G_TRY
    {
      DjVuDocument *doc = document->doc;
      if (! (doc && doc->is_init_ok()))
        return -1;
      GP<DjVmDir> dir = doc->get_djvm_dir();
      if (! dir)
        return 0;
      GP<DjVmDir::File> file;
      if (! (file = dir->id_to_file(GUTF8String(name))))
        if (! (file = dir->name_to_file(GUTF8String(name))))
          if (! (file = dir->title_to_file(GUTF8String(name))))
            {
              char *edata=0;
              long int p = strtol(name, &edata, 10);
              if (edata!=name && !*edata && p>=1)
                file = dir->page_to_file(p-1);
            }
      if (file)
        {
          int pageno = -1;
          int fileno = dir->get_file_pos(file);
          if (dir->pos_to_file(fileno, &pageno))
            return pageno;
        }
    }
  G_CATCH(ex)
    {
      ERROR1(document,ex);
    }
  G_ENDCATCH;
  return -1;
}



int 
ddjvu_document_check_pagedata(ddjvu_document_t *document, int pageno)
{
  G_TRY
    {
      document->want_pageinfo();
      DjVuDocument *doc = document->doc;
      if (doc && doc->is_init_ok())
        {
          bool dontcreate = false;
          if (doc->get_doc_type() == DjVuDocument::INDIRECT ||
              doc->get_doc_type() == DjVuDocument::OLD_INDEXED )
            {
              dontcreate = true;
              GURL url = doc->page_to_url(pageno);
              if (! url.is_empty())
                {
                  GUTF8String name = (const char*)url.fname();
                  GMonitorLock lock(&document->monitor);
                  if (document->names.contains(name))
                    dontcreate = false;
                }
            }
          GP<DjVuFile> file = doc->get_djvu_file(pageno, dontcreate);
          if (file && file->is_data_present())
            return 1;
        }
    }
  G_CATCH(ex)
    {
      ERROR1(document,ex);
    }
  G_ENDCATCH;
  return 0;
}


#undef ddjvu_document_get_pageinfo

extern "C" DDJVUAPI ddjvu_status_t
ddjvu_document_get_pageinfo(ddjvu_document_t *d, int p, ddjvu_pageinfo_t *i);

ddjvu_status_t
ddjvu_document_get_pageinfo(ddjvu_document_t *d, int p, ddjvu_pageinfo_t *i)
{
  // for binary backward compatibility with ddjvuapi<=17
  struct info17_s { int w; int h; int d; };
  return ddjvu_document_get_pageinfo_imp(d,p,i,sizeof(struct info17_s));
}

ddjvu_status_t
ddjvu_document_get_pageinfo_imp(ddjvu_document_t *document, int pageno, 
                                ddjvu_pageinfo_t *pageinfo, 
                                unsigned int infosz)
{
  G_TRY
    {
      ddjvu_pageinfo_t myinfo;
      memset(pageinfo, 0, infosz);
      if (infosz > sizeof(myinfo))
        return DDJVU_JOB_FAILED;
      DjVuDocument *doc = document->doc;
      if (doc)
        {
          document->want_pageinfo();
          GP<DjVuFile> file = doc->get_djvu_file(pageno);
          if (! file || ! file->is_data_present() )
            return DDJVU_JOB_STARTED;
          const GP<ByteStream> pbs(file->get_djvu_bytestream(false, false));
          const GP<IFFByteStream> iff(IFFByteStream::create(pbs));
          GUTF8String chkid;
          if (iff->get_chunk(chkid))
            {
              if (chkid == "FORM:DJVU")
                {
                  while (iff->get_chunk(chkid) && chkid!="INFO")
                    iff->close_chunk();
                  if (chkid == "INFO")
                    {
                      GP<ByteStream> gbs = iff->get_bytestream();
                      GP<DjVuInfo> info=DjVuInfo::create();
                      info->decode(*gbs);
                      int rot = info->orientation;
                      myinfo.rotation = rot;
                      myinfo.width = (rot&1) ? info->height : info->width;
                      myinfo.height = (rot&1) ? info->width : info->height;
                      myinfo.dpi = info->dpi;
                      myinfo.version = info->version;
                      memcpy(pageinfo, &myinfo, infosz);
                      return DDJVU_JOB_OK;
                    }
                }
              else if (chkid == "FORM:BM44" || chkid == "FORM:PM44")
                {
                  while (iff->get_chunk(chkid) && 
                         chkid!="BM44" && chkid!="PM44")
                    iff->close_chunk();
                  if (chkid=="BM44" || chkid=="PM44")
                    {
                      GP<ByteStream> gbs = iff->get_bytestream();
                      if (gbs->read8() == 0)
                        {
                          gbs->read8();
                          unsigned char vhi = gbs->read8();
                          unsigned char vlo = gbs->read8();
                          unsigned char xhi = gbs->read8();
                          unsigned char xlo = gbs->read8();
                          unsigned char yhi = gbs->read8();
                          unsigned char ylo = gbs->read8();
                          myinfo.width = (xhi<<8)+xlo;
                          myinfo.height = (yhi<<8)+ylo;
                          myinfo.dpi = 100;
                          myinfo.rotation = 0;
                          myinfo.version = (vhi<<8)+vlo;
                          memcpy(pageinfo, &myinfo, infosz);
                        }
                    }
                }
            }
        }
    }
  G_CATCH(ex)
    {
      ERROR1(document, ex);
    }
  G_ENDCATCH;
  return DDJVU_JOB_FAILED;
}


// ----------------------------------------
// Page

static ddjvu_page_t *
ddjvu_page_create(ddjvu_document_t *document, ddjvu_job_t *job,
                  const char *pageid, int pageno)
{
  ddjvu_page_t *p = 0;
  G_TRY
    {
      DjVuDocument *doc = document->doc;
      if (! doc) return 0;
      p = new ddjvu_page_s;
      ref(p);
      GMonitorLock lock(&p->monitor);
      p->myctx = document->myctx;
      p->mydoc = document;
      p->pageinfoflag = false;
      p->pagedoneflag = false;
      if (! job)
        job = p;
      p->job = job;
      if (pageid)
        p->img = doc->get_page(GNativeString(pageid), false, job);
      else
        p->img = doc->get_page(pageno, false, job);
    }
  G_CATCH(ex)
    {
      if (p)
        unref(p);
      p = 0;
      ERROR1(document, ex);
    }
  G_ENDCATCH;
  return p;
}

ddjvu_page_t *
ddjvu_page_create_by_pageno(ddjvu_document_t *document, int pageno)
{
  return ddjvu_page_create(document, 0, 0, pageno);
}

ddjvu_page_t *
ddjvu_page_create_by_pageid(ddjvu_document_t *document, const char *pageid)
{
  return ddjvu_page_create(document, 0, pageid, 0);
}

ddjvu_job_t *
ddjvu_page_job(ddjvu_page_t *page)
{
  return page;
}


// ----------------------------------------
// Page callbacks

void
ddjvu_page_s::release()
{
  img = 0;
}

ddjvu_status_t
ddjvu_page_s::status()
{
  if (! img)
    return DDJVU_JOB_NOTSTARTED;        
  DjVuFile *file = img->get_djvu_file();
  DjVuInfo *info = img->get_info();
  if (! file)
    return DDJVU_JOB_NOTSTARTED;
  else if (file->is_decode_stopped())
    return DDJVU_JOB_STOPPED;
  else if (file->is_decode_failed())
    return DDJVU_JOB_FAILED;
  else if (file->is_decode_ok())
    return (info) ? DDJVU_JOB_OK : DDJVU_JOB_FAILED;
  else if (file->is_decoding())
    return DDJVU_JOB_STARTED;
  return DDJVU_JOB_NOTSTARTED;
}

bool
ddjvu_page_s::inherits(const GUTF8String &classname)
{
  return (classname == "ddjvu_page_s")
    || ddjvu_job_s::inherits(classname);
}

bool 
ddjvu_page_s::notify_error(const DjVuPort *, const GUTF8String &m)
{
  if (!img) return false;
  msg_push(xhead(DDJVU_ERROR, this), msg_prep_error(m));
  return true;
}
 
bool 
ddjvu_page_s::notify_status(const DjVuPort *p, const GUTF8String &m)
{
  if (!img) return false;
  msg_push(xhead(DDJVU_INFO, this), msg_prep_info(m));
  return true;
}

void 
ddjvu_page_s::notify_file_flags_changed(const DjVuFile *sender, long, long)
{
  GMonitorLock lock(&monitor);
  if (!img) return;
  DjVuFile *file = img->get_djvu_file();
  if (file==0 || file!=sender) return;
  long flags = file->get_flags();
  if ((flags & DjVuFile::DECODE_OK) ||
      (flags & DjVuFile::DECODE_FAILED) ||
      (flags & DjVuFile::DECODE_STOPPED) )
    {
      if (pagedoneflag) return;
      msg_push(xhead(DDJVU_PAGEINFO, this));
      pageinfoflag = pagedoneflag = true;
    }
}

void 
ddjvu_page_s::notify_relayout(const DjVuImage *dimg)
{
  GMonitorLock lock(&monitor);
  if (img && !pageinfoflag)
    {
      msg_push(xhead(DDJVU_PAGEINFO, this));
      msg_push(xhead(DDJVU_RELAYOUT, this));
      pageinfoflag = true;
    }
}

void 
ddjvu_page_s::notify_redisplay(const DjVuImage *dimg)
{
  GMonitorLock lock(&monitor);
  if (img && !pageinfoflag)
    {
      msg_push(xhead(DDJVU_PAGEINFO, this));
      msg_push(xhead(DDJVU_RELAYOUT, this));
      pageinfoflag = true;
    }
  if (img && pageinfoflag)
    msg_push(xhead(DDJVU_REDISPLAY, this));
}

void 
ddjvu_page_s::notify_chunk_done(const DjVuPort*, const GUTF8String &name)
{
  GMonitorLock lock(&monitor);
  if (! img) return;
  GP<ddjvu_message_p> p = new ddjvu_message_p;
  p->tmp1 = name;
  p->p.m_chunk.chunkid = (const char*)(p->tmp1);
  msg_push(xhead(DDJVU_CHUNK,this), p);
}


// ----------------------------------------
// Page queries

int
ddjvu_page_get_width(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        return page->img->get_width();
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 0;
}

int
ddjvu_page_get_height(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        return page->img->get_height();
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 0;
}

int
ddjvu_page_get_resolution(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        return page->img->get_dpi();
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 0;
}

double
ddjvu_page_get_gamma(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        return page->img->get_gamma();
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 2.2;
}

int
ddjvu_page_get_version(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        return page->img->get_version();
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return DJVUVERSION;
}

ddjvu_page_type_t
ddjvu_page_get_type(ddjvu_page_t *page)
{
  G_TRY
    {
      if (! (page && page->img))
        return DDJVU_PAGETYPE_UNKNOWN;
      else if (page->img->is_legal_bilevel())
        return DDJVU_PAGETYPE_BITONAL;
      else if (page->img->is_legal_photo())
        return DDJVU_PAGETYPE_PHOTO;
      else if (page->img->is_legal_compound())
        return DDJVU_PAGETYPE_COMPOUND;
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return DDJVU_PAGETYPE_UNKNOWN;
}

char *
ddjvu_page_get_short_description(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        {
          const char *desc = page->img->get_short_description();
          return xstr(DjVuMessageLite::LookUpUTF8(desc));
        }
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 0;
}

char *
ddjvu_page_get_long_description(ddjvu_page_t *page)
{
  G_TRY
    {
      if (page && page->img)
        {
          const char *desc = page->img->get_long_description();
          return xstr(DjVuMessageLite::LookUpUTF8(desc));
        }
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 0;
}


// ----------------------------------------
// Rotations

void
ddjvu_page_set_rotation(ddjvu_page_t *page,
                        ddjvu_page_rotation_t rot)
{
  G_TRY
    {
      switch(rot)
        {
        case DDJVU_ROTATE_0:
        case DDJVU_ROTATE_90:
        case DDJVU_ROTATE_180:
        case DDJVU_ROTATE_270:
          if (page && page->img && page->img->get_info())
            page->img->set_rotate((int)rot);
          break;
        default:
          G_THROW("Illegal ddjvu rotation code");
          break;
        }
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
}

ddjvu_page_rotation_t
ddjvu_page_get_rotation(ddjvu_page_t *page)
{
  ddjvu_page_rotation_t rot = DDJVU_ROTATE_0;
  G_TRY
    {
      if (page && page->img)
        rot = (ddjvu_page_rotation_t)(page->img->get_rotate() & 3);
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return rot;
}

ddjvu_page_rotation_t
ddjvu_page_get_initial_rotation(ddjvu_page_t *page)
{
  ddjvu_page_rotation_t rot = DDJVU_ROTATE_0;
  G_TRY
    {
      GP<DjVuInfo> info;
      if (page && page->img)
        info = page->img->get_info();
      if (info)
        rot = (ddjvu_page_rotation_t)(info->orientation & 3);
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return rot;
}


// ----------------------------------------
// Rectangles

static void
rect2grect(const ddjvu_rect_t *r, GRect &g)
{
  g.xmin = r->x;
  g.ymin = r->y;
  g.xmax = r->x + r->w;
  g.ymax = r->y + r->h;
}

static void
grect2rect(const GRect &g, ddjvu_rect_t *r)
{
  if (g.isempty())
    {
      r->x = r->y = 0;
      r->w = r->h = 0;
    }
  else
    {
      r->x = g.xmin;
      r->y = g.ymin;
      r->w = g.width();
      r->h = g.height();
    }
}

ddjvu_rectmapper_t *
ddjvu_rectmapper_create(ddjvu_rect_t *input, ddjvu_rect_t *output)
{
  GRect ginput, goutput;
  rect2grect(input, ginput);
  rect2grect(output, goutput);
  GRectMapper *mapper = new GRectMapper;
  if (!ginput.isempty())
    mapper->set_input(ginput);
  if (!goutput.isempty())
    mapper->set_output(goutput);
  return (ddjvu_rectmapper_t*)mapper;
}

void
ddjvu_rectmapper_modify(ddjvu_rectmapper_t *mapper,
                        int rotation, int mirrorx, int mirrory)
{
  GRectMapper *gmapper = (GRectMapper*)mapper;
  if (! gmapper) return;
  gmapper->rotate(rotation);
  if (mirrorx & 1)
    gmapper->mirrorx();
  if (mirrory & 1)
    gmapper->mirrory();
}

void 
ddjvu_rectmapper_release(ddjvu_rectmapper_t *mapper)
{
  GRectMapper *gmapper = (GRectMapper*)mapper;
  if (! gmapper) return;
  delete gmapper;
}

void 
ddjvu_map_point(ddjvu_rectmapper_t *mapper, int *x, int *y)
{
  GRectMapper *gmapper = (GRectMapper*)mapper;
  if (! gmapper) return;
  gmapper->map(*x,*y);
}

void 
ddjvu_map_rect(ddjvu_rectmapper_t *mapper, ddjvu_rect_t *rect)
{
  GRectMapper *gmapper = (GRectMapper*)mapper;
  if (! gmapper) return;
  GRect grect;
  rect2grect(rect,grect);
  gmapper->map(grect);
  grect2rect(grect,rect);
}

void 
ddjvu_unmap_point(ddjvu_rectmapper_t *mapper, int *x, int *y)
{
  GRectMapper *gmapper = (GRectMapper*)mapper;
  if (! gmapper) return;
  gmapper->unmap(*x,*y);
}

void 
ddjvu_unmap_rect(ddjvu_rectmapper_t *mapper, ddjvu_rect_t *rect)
{
  GRectMapper *gmapper = (GRectMapper*)mapper;
  if (! gmapper) return;
  GRect grect;
  rect2grect(rect,grect);
  gmapper->unmap(grect);
  grect2rect(grect,rect);
}


// ----------------------------------------
// Render

struct DJVUNS ddjvu_format_s
{
  ddjvu_format_style_t style;
  uint32_t rgb[3][256];
  uint32_t palette[6*6*6];
  uint32_t xorval;
  double gamma;
  GPixel white;
  char ditherbits;
  bool rtoptobottom;
  bool ytoptobottom;
};

static ddjvu_format_t *
fmt_error(ddjvu_format_t *fmt)
{
  delete fmt;
  return 0;
}

ddjvu_format_t *
ddjvu_format_create(ddjvu_format_style_t style,
                    int nargs, unsigned int *args)
{
  ddjvu_format_t *fmt = new ddjvu_format_s;
  memset(fmt, 0, sizeof(ddjvu_format_t));
  fmt->style = style;  
  fmt->rtoptobottom = false;
  fmt->ytoptobottom = false;
  fmt->gamma = 2.2;
  fmt->white = GPixel::WHITE;
  // Ditherbits
  fmt->ditherbits = 32;
  if (style==DDJVU_FORMAT_RGBMASK16)
    fmt->ditherbits = 16;
  else if (style==DDJVU_FORMAT_PALETTE8)
    fmt->ditherbits = 8;
  else if (style==DDJVU_FORMAT_MSBTOLSB || style==DDJVU_FORMAT_LSBTOMSB)
    fmt->ditherbits = 1;
  // Args
  switch(style)
    {
    case DDJVU_FORMAT_RGBMASK16:
    case DDJVU_FORMAT_RGBMASK32: 
      {
        if (sizeof(uint16_t)!=2 || sizeof(uint32_t)!=4)
          return fmt_error(fmt);
        if (!args || nargs<3 || nargs>4)
          return fmt_error(fmt);
        { // extra nesting for windows
          for (int j=0; j<3; j++)
          {
            int shift = 0;
            uint32_t mask = args[j];
            for (shift=0; shift<32 && !(mask & 1); shift++)
              mask >>= 1;
            if ((shift>=32) || (mask&(mask+1)))
              return fmt_error(fmt);
            for (int i=0; i<256; i++)
              fmt->rgb[j][i] = (mask & ((int)((i*mask+127.0)/255.0)))<<shift;
          }
        }
        if (nargs >= 4)
          fmt->xorval = args[3];
        break;
      }
    case DDJVU_FORMAT_PALETTE8:
      {
        if (nargs!=6*6*6 || !args)
          return fmt_error(fmt);
        { // extra nesting for windows
          for (int k=0; k<6*6*6; k++)
            fmt->palette[k] = args[k];
        }
        { // extra nesting for windows
          int j=0;
          for(int i=0; i<6; i++)
            for(; j < (i+1)*0x33 - 0x19 && j<256; j++)
            {
              fmt->rgb[0][j] = i * 6 * 6;
              fmt->rgb[1][j] = i * 6;
              fmt->rgb[2][j] = i;
            }
        }
        break;
      }
    case DDJVU_FORMAT_RGB24:
	case DDJVU_FORMAT_BGRA:
    case DDJVU_FORMAT_BGR24:
    case DDJVU_FORMAT_GREY8:
    case DDJVU_FORMAT_LSBTOMSB:
    case DDJVU_FORMAT_MSBTOLSB:
      if (!nargs) 
        break;
    default:
      return fmt_error(fmt);
    }
  return fmt;
}

void
ddjvu_format_set_row_order(ddjvu_format_t *format, int top_to_bottom)
{
  format->rtoptobottom = !! top_to_bottom;
}

void
ddjvu_format_set_y_direction(ddjvu_format_t *format, int top_to_bottom)
{
  format->ytoptobottom = !! top_to_bottom;
}

void
ddjvu_format_set_ditherbits(ddjvu_format_t *format, int bits)
{
  if (bits>0 && bits<=64)
    format->ditherbits = bits;
}

void
ddjvu_format_set_gamma(ddjvu_format_t *format, double gamma)
{
  if (gamma>=0.5 && gamma<=5.0)
    format->gamma = gamma;
}

void
ddjvu_format_set_white(ddjvu_format_t *format, 
                       unsigned char b, unsigned char g, unsigned char r)
{
  format->white.b = b;
  format->white.g = g;
  format->white.r = r;
}

void
ddjvu_format_release(ddjvu_format_t *format)
{
  delete format;
}

static void
fmt_convert_row(const GPixel *p, int w, 
                const ddjvu_format_t *fmt, char *buf)
{
  const uint32_t (*r)[256] = fmt->rgb;
  const uint32_t xorval = fmt->xorval;
  switch(fmt->style)
    {
    case DDJVU_FORMAT_BGR24:    /* truecolor 24 bits in BGR order */
      {
        memcpy(buf, (const char*)p, 3*w);
        break;
      }
	case DDJVU_FORMAT_BGRA:    /* truecolor 24 bits in BGR order */
      {
        while (--w >= 0) { 
          buf[0]=p->b; buf[1]=p->g; buf[2]=p->r; buf[3]=255;
          buf+=4; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_RGB24:    /* truecolor 24 bits in RGB order */
      { 
        while (--w >= 0) { 
          buf[0]=p->r; buf[1]=p->g; buf[2]=p->b; 
          buf+=3; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_RGBMASK16: /* truecolor 16 bits with masks */
      {
        uint16_t *b = (uint16_t*)buf;
        while (--w >= 0) {
          b[0]=(r[0][p->r]|r[1][p->g]|r[2][p->b])^xorval; 
          b+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_RGBMASK32: /* truecolor 32 bits with masks */
      {
        uint32_t *b = (uint32_t*)buf;
        while (--w >= 0) {
          b[0]=(r[0][p->r]|r[1][p->g]|r[2][p->b])^xorval; 
          b+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_GREY8:    /* greylevel 8 bits */
      {
        while (--w >= 0) { 
          buf[0]=(5*p->r + 9*p->g + 2*p->b)>>4; 
          buf+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_PALETTE8: /* paletized 8 bits (6x6x6 color cube) */
      {
        const uint32_t *u = fmt->palette;
        while (--w >= 0) {
          buf[0] = u[r[0][p->r]+r[1][p->g]+r[2][p->b]]; 
          buf+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_MSBTOLSB: /* packed bits, msb on the left */
      {
        int t = (5*fmt->white.r + 9*fmt->white.g + 2*fmt->white.b + 16);
        t = t * 0xc / 0x10;
        unsigned char s=0, m=0x80;
        while (--w >= 0) {
          if ( 5*p->r + 9*p->g + 2*p->b < t ) { s |= m; }
          if (! (m >>= 1)) { *buf++ = s; s=0; m=0x80; }
          p += 1;
        }
        if (m < 0x80) { *buf++ = s; }
        break;
      }
    case DDJVU_FORMAT_LSBTOMSB: /* packed bits, lsb on the left */
      {
        int t = 5*fmt->white.r + 9*fmt->white.g + 2*fmt->white.b + 16;
        t = t * 0xc / 0x10;
        unsigned char s=0, m=0x1;
        while (--w >= 0) {
          if ( 5*p->r + 9*p->g + 2*p->b < t ) { s |= m; }
          if (! (m <<= 1)) { *buf++ = s; s=0; m=0x1; }
          p += 1;
        }
        if (m > 0x1) { *buf++ = s; }
        break;
      }
    }
}

static void
fmt_convert(GPixmap *pm, const ddjvu_format_t *fmt, char *buffer, int rowsize)
{
  int w = pm->columns();
  int h = pm->rows();
  // Loop on rows
  if (fmt->rtoptobottom)
    {
      for(int r=h-1; r>=0; r--, buffer+=rowsize)
        fmt_convert_row((*pm)[r], w, fmt, buffer);
    }
  else
    {
      for(int r=0; r<h; r++, buffer+=rowsize)
        fmt_convert_row((*pm)[r], w, fmt, buffer);
    }
}

static void
fmt_convert_row(unsigned char *p, unsigned char g[256][4], int w, 
                const ddjvu_format_t *fmt, char *buf)
{
  const uint32_t (*r)[256] = fmt->rgb;
  const uint32_t xorval = fmt->xorval;
  switch(fmt->style)
    {
    case DDJVU_FORMAT_BGR24:    /* truecolor 24 bits in BGR order */
      { 
        while (--w >= 0) { 
          buf[0]=g[*p][0];
          buf[1]=g[*p][1];
          buf[2]=g[*p][2];
          buf+=3; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_RGB24:    /* truecolor 24 bits in RGB order */
      { 
        while (--w >= 0) { 
          buf[0]=g[*p][2];
          buf[1]=g[*p][1];
          buf[2]=g[*p][0];
          buf+=3; p+=1; 
        }
        break;
      }
	case DDJVU_FORMAT_BGRA:    /* truecolor 24 bits in RGB order */
      {
        while (--w >= 0) { 
          buf[0]=g[*p][2];
          buf[1]=g[*p][1];
          buf[2]=g[*p][0];
		  buf[3]=255;
          buf+=4; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_RGBMASK16: /* truecolor 16 bits with masks */
      {
        uint16_t *b = (uint16_t*)buf;
        while (--w >= 0) {
          unsigned char x = *p;
          b[0]=(r[0][g[x][2]]|r[1][g[x][1]]|r[2][g[x][0]])^xorval; 
          b+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_RGBMASK32: /* truecolor 32 bits with masks */
      {
        uint32_t *b = (uint32_t*)buf;
        while (--w >= 0) {
          unsigned char x = *p;
          b[0]=(r[0][g[x][2]]|r[1][g[x][1]]|r[2][g[x][0]])^xorval; 
          b+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_GREY8:    /* greylevel 8 bits */
      {
        while (--w >= 0) { 
          buf[0]=g[*p][3];
          buf+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_PALETTE8: /* paletized 8 bits (6x6x6 color cube) */
      {
        const uint32_t *u = fmt->palette;
        while (--w >= 0) {
          unsigned char x = *p;
          buf[0] = u[r[0][g[x][0]]+r[1][g[x][1]]+r[2][g[x][2]]]; 
          buf+=1; p+=1; 
        }
        break;
      }
    case DDJVU_FORMAT_MSBTOLSB: /* packed bits, msb on the left */
      {
        int t = 5*fmt->white.r + 9*fmt->white.g + 2*fmt->white.b + 16;
        t = t * 0xc / 0x100;
        unsigned char s=0, m=0x80;
        while (--w >= 0) {
          unsigned char x = *p;
          if ( g[x][3] < t ) { s |= m; }
          if (! (m >>= 1)) { *buf++ = s; s=0; m=0x80; }
          p += 1;
        }
        if (m < 0x80) { *buf++ = s; }
        break;
      }
    case DDJVU_FORMAT_LSBTOMSB: /* packed bits, lsb on the left */
      {
        int t = 5*fmt->white.r + 9*fmt->white.g + 2*fmt->white.b + 16;
        t = t * 0xc / 0x100;
        unsigned char s=0, m=0x1;
        while (--w >= 0) {
          unsigned char x = *p;
          if ( g[x][3] < t ) { s |= m; }
          if (! (m <<= 1)) { *buf++ = s; s=0; m=0x1; }
          p += 1;
        }
        if (m > 0x1) { *buf++ = s; }
        break;
      }
    }
}

static void
fmt_convert(GBitmap *bm, const ddjvu_format_t *fmt, char *buffer, int rowsize)
{
  int w = bm->columns();
  int h = bm->rows();
  int m = bm->get_grays();
  // Gray levels
  int i;
  unsigned char g[256][4];
  const GPixel &wh = fmt->white;
  for (i=0; i<m; i++)
    {
      g[i][0] = wh.b - ( i * wh.b + (m - 1)/2 ) / (m - 1);
      g[i][1] = wh.g - ( i * wh.g + (m - 1)/2 ) / (m - 1);
      g[i][2] = wh.r - ( i * wh.r + (m - 1)/2 ) / (m - 1);
      g[i][3] = (5*g[i][2] + 9*g[i][1] + 2*g[i][0])>>4; 
    }
  for (i=m; i<256; i++)
    g[i][0] = g[i][1] = g[i][2] = g[i][3] = 0;
  
  // Loop on rows
  if (fmt->rtoptobottom)
    {
      for(int r=h-1; r>=0; r--, buffer+=rowsize)
        fmt_convert_row((*bm)[r], g, w, fmt, buffer);
    }
  else
    {
      for(int r=0; r<h; r++, buffer+=rowsize)
        fmt_convert_row((*bm)[r], g, w, fmt, buffer);
    }
}

static void
fmt_dither(GPixmap *pm, const ddjvu_format_t *fmt, int x, int y)
{
  if (fmt->ditherbits < 8)
    return;
  else if (fmt->ditherbits < 15)
    pm->ordered_666_dither(x, y);
  else if (fmt->ditherbits < 24)
    pm->ordered_32k_dither(x, y);
}


// ----------------------------------------

int
ddjvu_page_render(ddjvu_page_t *page,
                  const ddjvu_render_mode_t mode,
                  const ddjvu_rect_t *pagerect,
                  const ddjvu_rect_t *renderrect,
                  const ddjvu_format_t *format,
                  unsigned long rowsize,
                  char *imagebuffer )
{
  G_TRY
    {
      GP<GPixmap> pm;
      GP<GBitmap> bm;
      GRect prect, rrect;
      rect2grect(pagerect, prect);
      rect2grect(renderrect, rrect);
      if (format && format->ytoptobottom)
        {
          prect.ymin = renderrect->y + renderrect->h;
          prect.ymax = prect.ymin + pagerect->h;
          rrect.ymin = pagerect->y + pagerect->h;
          rrect.ymax = rrect.ymin + renderrect->h;
        }

      DjVuImage *img = page->img;
      if (img) 
        {
          switch (mode)
            {
            case DDJVU_RENDER_COLOR:
              pm = img->get_pixmap(rrect,prect, format->gamma,format->white);
              if (! pm) 
                bm = img->get_bitmap(rrect,prect);
              break;
            case DDJVU_RENDER_BLACK:
              bm = img->get_bitmap(rrect,prect);
              if (! bm)
                pm = img->get_pixmap(rrect,prect, format->gamma,format->white);
              break;
            case DDJVU_RENDER_MASKONLY:
              bm = img->get_bitmap(rrect,prect);
              break;
            case DDJVU_RENDER_COLORONLY:
              pm = img->get_pixmap(rrect,prect, format->gamma,format->white);
              break;
            case DDJVU_RENDER_BACKGROUND:
              pm = img->get_bg_pixmap(rrect,prect, format->gamma,format->white);
              break;
            case DDJVU_RENDER_FOREGROUND:
              pm = img->get_fg_pixmap(rrect,prect, format->gamma,format->white);
              if (! pm) 
                bm = img->get_bitmap(rrect,prect);
              break;
            }
        }
      if (pm)
        {
          int dx = rrect.xmin - prect.xmin;
          int dy = rrect.ymin - prect.xmin;
          fmt_dither(pm, format, dx, dy);
          fmt_convert(pm, format, imagebuffer, rowsize);
          return 2;
        }
      else if (bm)
        {
          fmt_convert(bm, format, imagebuffer, rowsize);
          return 1;
        }
    }
  G_CATCH(ex)
    {
      ERROR1(page, ex);
    }
  G_ENDCATCH;
  return 0;
}


// ----------------------------------------
// Thumbnails

void
ddjvu_thumbnail_p::callback(void *cldata)
{
  ddjvu_thumbnail_p *thumb = (ddjvu_thumbnail_p*)cldata;
  if (thumb->document)
    {
      GMonitorLock lock(&thumb->document->monitor);
      if (thumb->pool && thumb->pool->is_eof())
        {
          GP<DataPool> pool = thumb->pool;
          int size = pool->get_size();
          thumb->pool = 0;
          G_TRY
            {
              thumb->data.resize(0,size-1);
              pool->get_data( (void*)(char*)thumb->data, 0, size);
            }
          G_CATCH_ALL
            {
              thumb->data.empty();
            }
          G_ENDCATCH;
          if (thumb->document->doc)
            {
              GP<ddjvu_message_p> p = new ddjvu_message_p;
              p->p.m_thumbnail.pagenum = thumb->pagenum;
              msg_push(xhead(DDJVU_THUMBNAIL, thumb->document), p);
            } 
        }
    }
}

ddjvu_status_t
ddjvu_thumbnail_status(ddjvu_document_t *document, int pagenum, int start)
{
  G_TRY
    {
      GP<ddjvu_thumbnail_p> thumb;
      DjVuDocument* doc = document->doc;
      if (doc)
        {
          GMonitorLock lock(&document->monitor);
          GPosition p = document->thumbnails.contains(pagenum);
          if (p)
            thumb = document->thumbnails[p];
        }
      if (!thumb && doc)
        {
          GP<DataPool> pool = doc->get_thumbnail(pagenum, !start);
          if (pool)
            {
              GMonitorLock lock(&document->monitor);
              thumb = new ddjvu_thumbnail_p;
              thumb->document = document;
              thumb->pagenum = pagenum;
              thumb->pool = pool;
              document->thumbnails[pagenum] = thumb;
            }
          if (thumb)
            pool->add_trigger(-1, ddjvu_thumbnail_p::callback, 
                              (void*)(ddjvu_thumbnail_p*)thumb);
        } 
      if (! thumb)
        return DDJVU_JOB_NOTSTARTED;        
      else if (thumb->pool)
        return DDJVU_JOB_STARTED;
      else if (thumb->data.size() > 0)
        return DDJVU_JOB_OK;
    }
  G_CATCH(ex)
    {
      ERROR1(document, ex);
    }
  G_ENDCATCH;
  return DDJVU_JOB_FAILED;
}
 
int
ddjvu_thumbnail_render(ddjvu_document_t *document, int pagenum, 
                       int *wptr, int *hptr,
                       const ddjvu_format_t *format,
                       unsigned long rowsize,
                       char *imagebuffer)
{
  G_TRY
    {
      GP<ddjvu_thumbnail_p> thumb;
      ddjvu_status_t status = ddjvu_thumbnail_status(document,pagenum,FALSE);
      if (status == DDJVU_JOB_OK)
        {
          GMonitorLock lock(&document->monitor);
          thumb = document->thumbnails[pagenum];
        }
      if (! (thumb && wptr && hptr))
        return FALSE;
      if (! (thumb->data.size() > 0))
        return FALSE;
      /* Decode wavelet data */
      int size = thumb->data.size();
      char *data = (char*)thumb->data;
      GP<IW44Image> iw = IW44Image::create_decode();
      iw->decode_chunk(ByteStream::create_static((void*)data, size));
      int w = iw->get_width();
      int h = iw->get_height();
      /* Restore aspect ratio */
      double dw = (double)w / *wptr;
      double dh = (double)h / *hptr;
      if (dw > dh) 
        *hptr = (int)(h / dw);
      else
        *wptr = (int)(w / dh);
      if (! imagebuffer)
        return TRUE;
      /* Render and scale image */
      GP<GPixmap> pm = iw->get_pixmap();
      double thumbgamma = document->doc->get_thumbnails_gamma();
      pm->color_correct(format->gamma/thumbgamma, format->white);
      GP<GPixmapScaler> scaler = GPixmapScaler::create(w, h, *wptr, *hptr);
      GP<GPixmap> scaledpm = GPixmap::create();
      GRect scaledrect(0, 0, *wptr, *hptr);
      scaler->scale(GRect(0, 0, w, h), *pm, scaledrect, *scaledpm);
      /* Convert */
      fmt_dither(scaledpm, format, 0, 0);
      fmt_convert(scaledpm, format, imagebuffer, rowsize);
      return TRUE;
    }
  G_CATCH(ex)
    {
      ERROR1(document, ex);
    }
  G_ENDCATCH;
  return FALSE;
}

// ----------------------------------------
// Backdoors

GP<DjVuImage>
ddjvu_get_DjVuImage(ddjvu_page_t *page)
{
  return page->img;
}


GP<DjVuDocument>
ddjvu_get_DjVuDocument(ddjvu_document_t *document)
{
  return document->doc;
}


