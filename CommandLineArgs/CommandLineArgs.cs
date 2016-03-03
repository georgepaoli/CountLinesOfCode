using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    // TODO: make it generic?
    // TODO: This is the weirdest collection design - recheck in few days
    // TODO: maybe this should be few filtering IEnumerable<CommandLineArg> methods? (that would make it immutable and perhaps also slower - does it even matter?)
    // TODO: although i feel like these stages in here are pretty useful
    // TODO: either way it will get me closer to the cleaner design
    // TODO: seems like identical class can be used for params
    // TODO: can this be replaced with linked list?
    /// <summary>
    /// Warning: This class is intended to only be used in a foreach loop
    /// </summary>
    public class CommandLineArgs : IEnumerable<CommandLineArg>, IEnumerator<CommandLineArg>
    {
        private List<CommandLineArg> _args = new List<CommandLineArg>();
        private List<CommandLineArg> _nextWave = new List<CommandLineArg>();

        // TODO: add more resistance to improper usage if the design is ok
        //private bool _isInUse = false;
        private int _position = -1;
        private bool _forceNextWave = false;

        public CommandLineArg Current { get { return _args[_position]; } }
        public void ProcessCurrentArgLater()
        {
            _nextWave.Add(Current);
        }

        public void ForceNextWave()
        {
            _forceNextWave = true;
        }

        public void AddArgs(string[] args)
        {
            foreach (var arg in args)
            {
                _args.Add(new CommandLineArg(arg));
            }
        }

        public void Reset()
        {
            _position = -1;
            _forceNextWave = false;
        }

        // TODO: however confusing it looks this disposes only the enumerator and not the collection
        public void Dispose()
        {
            // TODO: this code was not originally intended
            for (; _position < _args.Count; _position++)
            {
                _nextWave.Add(_args[_position]);
            }

            var tmp = _args;
            _args = _nextWave;
            _nextWave = tmp;
            _nextWave.Clear();
        }

        public IEnumerator<CommandLineArg> GetEnumerator()
        {
            Reset();
            return this;
        }

        public bool MoveNext()
        {
            ++_position;
            if (_forceNextWave)
            {
                return false;
            }

            return _position < _args.Count;
        }

        public CommandLineArg PeekNext()
        {
            int positionOfNext = _position + 1;
            if (positionOfNext < _args.Count)
            {
                return _args[positionOfNext];
            }

            return null;
        }

        public bool Skip()
        {
            return ++_position < _args.Count;
        }

        public bool Empty { get { return _args.Count == 0; } }

        object IEnumerator.Current { get { return Current; } }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
