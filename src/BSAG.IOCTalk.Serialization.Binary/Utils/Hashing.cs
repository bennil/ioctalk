using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.Utils
{
    public static class Hashing
    {

        private const uint PrimeNo = 16777619;


        /// <summary>
        /// Creates the FNV hash.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>System.Int32.</returns>
        public static uint CreateHash(string data, uint seed = 151)
        {
            uint hash = seed;

            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];

                hash = hash ^ c;
                hash = hash * PrimeNo;
            }

            return hash;
        }

        /// <summary>
        /// Creates the FNV hash.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>System.Int32.</returns>
        public static uint CreateHash(uint number, uint seed = 151)
        {
            uint hash = seed;
            hash = hash ^ number;
            hash = hash * PrimeNo;
            
            return hash;
        }



        // Source: http://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
        //private const int HashSeed = 1009;
        //private const int HashFactor = 9176;

        //public static int CustomHash(params int[] vals)
        //{
        //    return CustomHash(HashSeed, vals);
        //}

        //public static int CustomHash(int seed, params int[] vals)
        //{
        //    int hash = seed;
        //    foreach (int i in vals)
        //    {
        //        hash = (hash * HashFactor) + i;
        //    }
        //    return hash;
        //}

        //public static int CustomHash(string data)
        //{
        //    return CustomHash(HashSeed, data);
        //}
        //public static int CustomHash(int seed, string data)
        //{
        //    int hash = seed;
        //    foreach (char i in data)
        //    {
        //        hash = (hash * HashFactor) + i;
        //    }
        //    return hash;
        //}

        //public static int CustomHash(string data, params int[] intValues)
        //{
        //    int hash = HashSeed;
        //    foreach (int i in intValues)
        //    {
        //        hash = (hash * HashFactor) + i;
        //    }

        //    foreach (char i in data)
        //    {
        //        hash = (hash * HashFactor) + i;
        //    }
        //    return hash;
        //}
    }
}
