namespace GetGUID.Editor
{
    internal class AuxContainer<TMain, TAux>
    {
        // ReSharper disable once InconsistentNaming
        internal TMain main { get; }
        // ReSharper disable once InconsistentNaming
        internal TAux aux { get; }

        internal AuxContainer(TMain main, TAux aux)
        {
            this.main = main;
            this.aux = aux;
        }

        internal AuxContainer<TNewMain, TAux> Map<TNewMain>(System.Func<TMain, TNewMain> fn) => new(fn(main), aux);

    }

    internal static class AuxContainer
    {
        internal static AuxContainer<TMain, TAux> Create<TMain, TAux>(TMain main, TAux aux) => new(main, aux);
    }
}
