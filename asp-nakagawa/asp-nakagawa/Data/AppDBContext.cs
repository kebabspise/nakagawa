using asp_nakagawa.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace asp_nakagawa.Data
{
    public class AppDBContext:DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Work_log> Work_logs { get; set; }
        public DbSet<Shift_request> Shift_requests { get; set; }
        public DbSet<Shift_confirm> Shift_confirms { get; set; }

    }
}
