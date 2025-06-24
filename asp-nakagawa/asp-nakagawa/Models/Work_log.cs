using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asp_nakagawa.Models
{
    public class Work_log
    {
        [Key]
        public int Id { get; set; }
        public DateTime? Work_start { get; set; }
        public DateTime? Work_end { get; set; }

        public int UserId { get; set; }  // 外部キー

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
