using asp_nakagawa.Models;
using System;
using System.Linq;

namespace asp_nakagawa.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDBContext context)
        {
            if (!context.Users.Any())
            {
                var sampleUser = new User
                {
                    user_id = 1,
                    name = "谷川様",
                    pass = "pass",
                    admin = true,
                    wages = 2500
                };
                context.Users.Add(sampleUser);
                context.SaveChanges();  
            }

            var user = context.Users.First();

            if (!context.Work_logs.Any())
            {
                context.Work_logs.Add(new Work_log
                {
                    work_start = DateTime.Now,
                    work_end = DateTime.Now,
                    user_id = user.id
                });
                context.SaveChanges();
            }

            if (!context.Shift_requests.Any())
            {
                context.Shift_requests.Add(new Shift_request
                {
                    work_start = DateTime.Now,
                    work_end = DateTime.Now,
                    user_id = user.id, 
                });
                context.SaveChanges();
            }

            if (!context.Shift_confirms.Any())
            {
                context.Shift_confirms.Add(new Shift_confirm
                {
                    work_start = DateTime.Now,
                    work_end = DateTime.Now,
                    user_id = user.id 
                });
                context.SaveChanges();
            }
        }
    }
}