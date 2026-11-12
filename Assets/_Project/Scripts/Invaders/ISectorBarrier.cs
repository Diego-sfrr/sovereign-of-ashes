using SovereignOfAshes.Board;

namespace SovereignOfAshes.Invaders
{
    /// <summary>
    /// Pode impedir um invasor de entrar em um setor durante a marcha (ex.: um Golem de Ferro).
    /// Definida aqui para que a fase da marcha dependa só desta abstração (DIP), sem conhecer a alquimia.
    /// </summary>
    public interface ISectorBarrier
    {
        /// <summary>
        /// Avalia se o setor de destino barra o invasor. Se barrar, a implementação aplica as consequências
        /// (dano de volta, desgaste) antes de devolver <c>true</c>.
        /// </summary>
        /// <param name="invader">Invasor que tenta entrar.</param>
        /// <param name="destination">Setor que ele quer ocupar.</param>
        /// <returns><c>true</c> se o invasor foi barrado e deve esperar.</returns>
        bool TryBlock(InvaderEntity invader, TectonicSector destination);
    }
}
