using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.ServiceHandler
{
    public static class ServiceCollectionExtension
    {
        public static async Task<PaginationDetails<T>> ToPaginatedResultAsync<T>(
            this IQueryable<T> query,
            int offset,
            int limit,
            CancellationToken cancellationToken = default,
            string? orderByColumn = null,
            bool sortAscending = false) where T : class
        {
            var totalSize = await query.CountAsync(cancellationToken);

            if (!string.IsNullOrEmpty(orderByColumn))
            {
                var parameter = Expression.Parameter(typeof(T), "t");
                var property = Expression.Property(parameter, orderByColumn);
                var lambda = Expression.Lambda(property, parameter);

                var methodName = sortAscending ? "OrderBy" : "OrderByDescending";
                var orderByMethod = typeof(Queryable).GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), property.Type);

                query = (IQueryable<T>)orderByMethod.Invoke(null, new object[] { query, lambda });
            }
            else if (!query.IsOrdered())// ID ordering as fallback
            {
                var idProp = typeof(T).GetProperty("Id");
                if (idProp != null)
                {
                    query = query.OrderBy(x => EF.Property<object>(x, "Id"));
                }
            }

            var data = await query
                .Skip(offset)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return new PaginationDetails<T>
            {
                Limit = limit,
                OffSet = offset,
                TotalSize = totalSize,
                HasMore = (offset + limit) < totalSize,
                Data = data
            };
        }

        // Helper extension to detect existing ordering
        public static bool IsOrdered<T>(this IQueryable<T> query)
        {
            return query.Expression.Type == typeof(IOrderedQueryable<T>);
        }
    }
}
