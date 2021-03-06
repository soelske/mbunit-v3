// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Portions Copyright 2000-2004 Jonathan de Halleux
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Gallio.Tests;
using MbUnit.Framework;

using Gallio.Model.Filters;

namespace Gallio.Tests.Model.Filters
{
    [TestFixture]
    [TestsOn(typeof(NoneFilter<object>))]
    public class NoneFilterTest : BaseTestWithMocks
    {
        [Test]
        public void IsMatchReturnsFalse()
        {
            Assert.IsFalse(new NoneFilter<object>().IsMatch(null));
        }

        [Test]
        [Row("")]
        [Row("id1212")]
        public void ToStringTest(string id)
        {
            NoneFilter<object> filter = new NoneFilter<object>();
            Assert.AreEqual(filter.ToString(), "None()");
        }
    }
}