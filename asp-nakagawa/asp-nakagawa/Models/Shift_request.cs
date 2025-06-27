using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asp_nakagawa.Models
{
    public class Shift_request
    {
        [Key]
        public int id { get; set; }
        public DateTime? work_start { get; set; }
        public DateTime? work_end { get; set; }

        [DataType(DataType.Date)]
        public DateTime? work_date { get; set; }

        [ForeignKey("User")]
        public int user_id { get; set; }  // 外部キー

        public User? User { get; set; }

    }
}
