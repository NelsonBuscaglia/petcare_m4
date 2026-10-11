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
    public const string IntervaloFueraDeRango = "N debe ser un número entero entre 1 y 23.";
    public const string MaxBanosInvalido = "El máximo de días entre baños debe ser un número entero mayor que 0.";
    public const string FechaPasada = "La fecha no puede ser anterior a hoy.";
    public const string SinSubtipo = "Elegí el subtipo: veterinario, estudio, vacuna o intervención médica.";
    public const string HoraFutura = "La hora no puede ser posterior a la actual.";
    public const string YaCumplida = "Esta tarea ya fue cumplida.";
    public const string YaDadaDeBaja = "Esta tarea ya fue dada de baja.";
    public const string SoloRegistrosDeHoy = "Solo se pueden eliminar registros de hoy.";
    public const string TareaInexistente = "Esa tarea no existe.";
}
