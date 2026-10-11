namespace PetCare.Web.Dominio;

public enum TipoDeber { Comida, Paseo, Medicacion, Bano, VisitaEstudio }

public enum SubtipoVisita { Veterinario, Estudio, Vacuna, IntervencionMedica }

public enum Modalidad { HoraFija, CadaNHoras }

public enum Recurrencia { SoloHoy, TodosLosDias }

public enum EstadoInstancia { Vigente, Cumplida, DadaDeBaja }

public enum TipoNotificacion { Vencimiento, Recordatorio, AvisoTurnoBano }

public enum EstadoNotificacion { Pendiente, Aceptada, Perdida }
