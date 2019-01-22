using FLGrains.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains
{
    class ConnectionStringProvider : IConnectionStringProvider
    {
        public string ConnectionString { get; private set; }


        public ConnectionStringProvider(string connectionString)
        {
            this.ConnectionString = connectionString;
        }
    }
}
