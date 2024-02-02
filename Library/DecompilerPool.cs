using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Library.Pull
{
    public class DecompilerPool
    {
        private readonly ConcurrentQueue<CSharpDecompiler> _pool;
        private readonly int _maxSize;
        private int _currentCount;
        private string _path;

        public DecompilerPool(string path, string assemblyName, int maxSize)
        {
            _path = Path.Combine(path, assemblyName);
            _maxSize = maxSize;
            _pool = new ConcurrentQueue<CSharpDecompiler>();
            _currentCount = 0;

            // Инициализация пула с начальным количеством объектов
            for (int i = 0; i < maxSize; i++)
            {
                var decompiler = new CSharpDecompiler(_path, new DecompilerSettings());
                _pool.Enqueue(decompiler);
                _currentCount++;
            }
        }

        public CSharpDecompiler GetDecompiler()
        {
            if (_pool.TryDequeue(out var decompiler))
            {
                return decompiler;
            }

            if (_currentCount < _maxSize)
            {
                Interlocked.Increment(ref _currentCount);
                return new CSharpDecompiler(_path, new DecompilerSettings());
            }

            return null; // Или блокировка до получения доступного декомпилятора
        }

        public void ReturnDecompiler(CSharpDecompiler decompiler)
        {
            _pool.Enqueue(decompiler);
        }
    }
}
