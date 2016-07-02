using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace DjvuApp.Model
{
    public sealed class EfBookDto
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        public DateTime LastOpeningTime { get; set; }

        public uint? LastOpenedPage { get; set; }

        public uint PageCount { get; set; }

        [Required, MaxLength(255)]
        public string BookPath { get; set; }

        [Required, MaxLength(255)]
        public string ThumbnailPath { get; set; } = "placeholder_to_prevent_x:Bind_error";

        public ICollection<EfBookmarkDto> Bookmarks { get; set; } = new List<EfBookmarkDto>();
    }
}