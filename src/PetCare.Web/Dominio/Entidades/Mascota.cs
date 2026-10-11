namespace PetCare.Web.Dominio.Entidades;

public class Mascota
{
    public int Id { get; set; }

    public int HogarId { get; set; }

    public Hogar? Hogar { get; set; }

    /// <summary>Obligatorio.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Nombre sin distinguir mayúsculas; único por hogar (FR-007).</summary>
    public string NombreNormalizado { get; set; } = "";

    /// <summary>Base del aviso de baño cuando no hay baños registrados (FR-040).</summary>
    public DateOnly CreadaDia { get; set; }

    /// <summary>Opcional; una vez definido se puede modificar pero no quitar (FR-008).</summary>
    public int? MaxDiasEntreBanos { get; set; }
}
