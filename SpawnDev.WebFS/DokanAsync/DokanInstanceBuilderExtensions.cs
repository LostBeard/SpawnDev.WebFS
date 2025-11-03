using DokanNet;

namespace SpawnDev.WebFS.DokanAsync
{
    public static class DokanInstanceBuilderExtensions
    {
        public static DokanInstance Build(this DokanInstanceBuilder _this, IAsyncDokanOperations asyncOperations)
        {
            var operations = new AsyncDokanOperations(asyncOperations);
            return _this.Build(operations);
        }
    }
}
