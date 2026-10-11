namespace PetCare.Web.Dominio.Entidades;

public class Cuidador
{
    public int Id { get; set; }

    public int HogarId { get; set; }

    public Hogar? Hogar { get; set; }

    /// <summary>Tal como se ingresó.</summary>
    public string Email { get; set; } = "";

    /// <summary><c>ToUpperInvariant</c> del email; único (FR-003).</summary>
    public string EmailNormalizado { get; set; } = "";

    /// <summary>Hash de <c>PasswordHasher&lt;Cuidador&gt;</c> (FR-047).</summary>
    public string PasswordHash { get; set; } = "";

    public DateTime CreadoUtc { get; set; }
}
