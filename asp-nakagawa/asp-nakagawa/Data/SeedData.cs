using asp_nakagawa.Models;
using System;

namespace asp_nakagawa.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDBContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    Name = "Sample",
                    Pass = "pass",
                    Admin = false,
                    Wages = 1000
                });
                context.SaveChanges();
            }

            var user = context.Users.First();

            if (!context.Work_logs.Any())
            {
                context.Work_logs.Add(new Work_log
                {
                    Work_start = DateTime.Now,
                    Work_end = DateTime.Now,
                    UserId = user.Id  // 外部キーを設定
                });
                context.SaveChanges();
            }

            if (!context.Shift_requests.Any())
            {
                context.Shift_requests.Add(new Shift_request
                {
                    Work_start = DateTime.Now,
                    Work_end = DateTime.Now,
                    UserId = user.Id  // 外部キーを設定
                });
                context.SaveChanges();
            }

            if (!context.Shift_confirms.Any())
            {
                context.Shift_confirms.Add(new Shift_confirm
                {
                    Work_start = DateTime.Now,
                    Work_end = DateTime.Now,
                    UserId = user.Id  // 外部キーを設定
                });
                context.SaveChanges();
            }

        }
    }
}
