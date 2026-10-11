namespace PetCare.Web.Servicios;

/// <summary>Mensajes de rechazo (contracts/ui-routes.md, FR-051).</summary>
public static class Mensajes
{
    public const string CodigoIncorrecto = "El código de hogar es incorrecto.";
    public const string HogarExistente = "El hogar ya existe: para sumarte necesitás su código.";
    public const string EmailRegistrado = "Ese email ya está registrado.";
    public const string PasswordCorta = "La contraseña debe tener al menos 8 caracteres.";
    public const string LoginFallido = "El email o la contraseña son incorrectos.";
    public const string SinSesion = "Iniciá sesión para continuar.";
    public const string MascotaSinNombre = "El nombre de la mascota no puede estar vacío.";
    public const string MascotaRepetida = "Ya existe una mascota con ese nombre.";
}
