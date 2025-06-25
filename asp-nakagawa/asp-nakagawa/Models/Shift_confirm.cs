using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asp_nakagawa.Models
{
    public class Shift_confirm
    {
        [Key]
        public int id { get; set; }
        public DateTime? work_start { get; set; }
        public DateTime? work_end { get; set; }

        public int user_id { get; set; }  // 外部キー
        public User User { get; set; }

    }
}
