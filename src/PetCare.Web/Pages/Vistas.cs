using PetCare.Web.Dominio;
using PetCare.Web.Servicios;

namespace PetCare.Web.Pages;

/// <summary>Sección de una mascota en el estado del día, con el motivo del último rechazo si lo hubo (FR-051).</summary>
public sealed record SeccionMascotaVista(SeccionMascota Seccion, string? Error = null);

/// <summary>
/// Formulario para confirmar un cumplimiento. <paramref name="HoraPropuesta"/> es la hora actual
/// para comida, paseo y medicación, y null para baño y visita/estudio, que no piden hora (FR-025, FR-027).
/// </summary>
public sealed record FormCumplirVista(int InstanciaId, int MascotaId, TipoDeber Tipo, TimeOnly Hora, TimeOnly? HoraPropuesta);
