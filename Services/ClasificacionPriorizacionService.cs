using TFU_Architecture.Models;

namespace TFU_Architecture.Services;

// Componente: Clasificación y Priorización - expone IClasificacion
public interface IClasificacionService
{
    NivelPrioridad Clasificar(int gravedad);
}

public class ClasificacionPriorizacionService : IClasificacionService
{
    public NivelPrioridad Clasificar(int gravedad) => gravedad switch
    {
        >= 9 => NivelPrioridad.Critica,
        >= 6 => NivelPrioridad.Alta,
        >= 3 => NivelPrioridad.Media,
        _ => NivelPrioridad.Baja
    };
}
