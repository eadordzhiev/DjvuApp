using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using DjvuApp.Model;

namespace DjvuApp.Migrations
{
    [DbContext(typeof(EfBooksContext))]
    partial class EfBooksContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("DjvuApp.Model.EfBookDto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BookPath");

                    b.Property<uint?>("LastOpenedPage");

                    b.Property<DateTime>("LastOpeningTime");

                    b.Property<uint>("PageCount");

                    b.Property<string>("ThumbnailPath");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.ToTable("Books");
                });

            modelBuilder.Entity("DjvuApp.Model.EfBookmarkDto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("EfBookDtoId");

                    b.Property<uint>("PageNumber");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.HasIndex("EfBookDtoId");

                    b.ToTable("Bookmarks");
                });

            modelBuilder.Entity("DjvuApp.Model.EfBookmarkDto", b =>
                {
                    b.HasOne("DjvuApp.Model.EfBookDto", "EfBookDto")
                        .WithMany("Bookmarks")
                        .HasForeignKey("EfBookDtoId");
                });
        }
    }
}
