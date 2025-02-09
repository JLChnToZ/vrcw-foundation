namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        interface IResolverCommand {
            ICommandState CreateState();
            void Reset(ICommandState state, object from);
            void Next(ICommandState state);
        }
    }
}