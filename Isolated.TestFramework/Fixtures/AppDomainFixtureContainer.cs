using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Isolated.TestFramework.Fixtures
{
    internal class AppDomainFixtureContainer : LongLivedMarshalByRefObject, IDisposable
    {
        private readonly IEnumerable<Type> _fixtureTypes;
        private readonly ExceptionAggregator _aggregator;
        private readonly Dictionary<Type, object> _fixtures = new Dictionary<Type, object>();

        public AppDomainFixtureContainer(IEnumerable<Type> fixtureTypes, ExceptionAggregator aggregator)
        {
            _fixtureTypes = fixtureTypes;
            _aggregator = aggregator;
        }

        public void CreateFixtures()
        {
            foreach (var fixtureType in _fixtureTypes)
            {
                _aggregator.Run(() =>
                {
                    var fixture = ObjectFactory.CreateInstance(fixtureType, null);
                    _fixtures.Add(fixtureType, fixture);
                });
            }
        }

        public void Dispose()
        {
            foreach (var fixture in _fixtures.Values.OfType<IDisposable>())
            {
                _aggregator.Run(() => fixture.Dispose());
            }
            _fixtures.Clear();
        }
    }
}