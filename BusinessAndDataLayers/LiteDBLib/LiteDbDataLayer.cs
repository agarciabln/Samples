﻿using LiteDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiteDBLib
{
    public class LiteDbDataLayer
    {
        private readonly LiteDatabase _db;

        public LiteDbDataLayer(LiteDatabase db)
        {
            _db = db;
        }

        public Task<IAsyncEnumerable<object>> GetAsync(Expression predicate)
        {
            var recordType = predicate.Type.GenericTypeArguments[0];

            var getCollectionMethod = typeof(LiteDatabase).GetMethod(nameof(LiteDatabase.GetCollection), new Type[] { }).MakeGenericMethod(recordType);

            var queryMethod = typeof(ILiteCollection<>).MakeGenericType(recordType).GetMethod(nameof(ILiteCollection<object>.Query));

            //TODO: this is pretty horrible
            var whereMethod = typeof(LiteQueryable<>).MakeGenericType(recordType).GetMethods().FirstOrDefault(m =>
             {
                 var parameters = m.GetParameters();

                 var firstParameter = parameters.First();

                 if (m.Name != nameof(LiteQueryable<object>.Where)) return false;

                 return firstParameter.ParameterType.Name == "Expression`1";
             });

            var toListMethod = typeof(LiteQueryable<>).MakeGenericType(recordType).GetMethod(nameof(LiteQueryable<object>.ToList));

            //Get the collection
            var liteCollection = getCollectionMethod.Invoke(_db, null);

            //Get the queryable
            var liteQueryable = queryMethod.Invoke(liteCollection, null);

            //Get queryable with a where clause
            var liteQueryableWithWhere = whereMethod.Invoke(liteQueryable, new object[] { predicate });

            //Perform the query
            var list = (IList)toListMethod.Invoke(liteQueryableWithWhere, null);

            //Cast to a list of objects
            var objects = list.Cast<object>();

            return Task.FromResult(objects.ToAsyncEnumerable());
        }
    }
}
