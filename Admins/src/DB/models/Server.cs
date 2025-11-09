using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Admins.Contract;

namespace Admins.Database.Models;

[Table("Servers")]
public class Server : IServer
{
    [Key]
    public ulong Id { get; set; }

    [Column("IP")]
    public string IP { get; set; } = string.Empty;

    [Column("Port")]
    public int Port { get; set; }

    [Column("GUID")]
    public string GUID { get; set; } = string.Empty;
}
