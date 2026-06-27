using System.Reflection;

namespace BudgetApp.Reflection
{
    public static class ReflectionMapper
    {
        public static void Map<TTarget, TSource>(TTarget target, TSource source)
        {
            var targetProps = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var tProp in targetProps)
            {
                var sProp = sourceProps.FirstOrDefault(p => p.Name == tProp.Name && p.PropertyType == tProp.PropertyType);

                if (sProp != null)
                {
                    var value = sProp.GetValue(source);
                    tProp.SetValue(target, value);
                }
            }
        }
    }
}