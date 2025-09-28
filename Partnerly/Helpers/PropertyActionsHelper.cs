namespace Partnerly.Helpers
{
    public class PropertyActionsHelper
    {
        public static async Task<TDestination> CopyPropertiesAsync<TSource, TDestination>(
     TSource source,
     TDestination destination
 ) where TDestination : new()
        {
            if (destination == null) destination = new TDestination();
            if (source == null) return destination;

            return await Task.Run(() =>
            {
                var sourceProps = typeof(TSource).GetProperties()
                    .Where(p => p.CanRead)
                    .ToList();

                var destProps = typeof(TDestination).GetProperties()
                    .Where(p => p.CanWrite)
                    .ToList();

                foreach (var sProp in sourceProps)
                {
                    var dProp = destProps.FirstOrDefault(p => p.Name == sProp.Name && p.PropertyType == sProp.PropertyType);
                    if (dProp != null)
                    {
                        var value = sProp.GetValue(source);
                        dProp.SetValue(destination, value);
                    }
                }

                return destination;
            });
        }


        public static async Task<List<TDestination>> CopyPropertiesListAsync<TSource, TDestination>(
        List<TSource> source,
        List<TDestination> destination
        ) where TDestination : new()
        {
            if (destination == null) destination = new List<TDestination>();
            if (source == null) return destination;

            return await Task.Run(() =>
            {
                for (int i = 0; i < source.Count; i++)
                {
                    TSource src = source[i];
                    TDestination dest;

                    if (i < destination.Count)
                    {
                        dest = destination[i];
                    }
                    else
                    {
                        dest = new TDestination();
                        destination.Add(dest);
                    }

                    var sourceProps = typeof(TSource).GetProperties()
                        .Where(p => p.CanRead)
                        .ToList();

                    var destProps = typeof(TDestination).GetProperties()
                        .Where(p => p.CanWrite)
                        .ToList();

                    foreach (var sProp in sourceProps)
                    {
                        var dProp = destProps.FirstOrDefault(p => p.Name == sProp.Name && p.PropertyType == sProp.PropertyType);
                        if (dProp != null)
                        {
                            var value = sProp.GetValue(src);
                            dProp.SetValue(dest, value);
                        }
                    }
                }

                return destination;
            });
        }



    }
}
