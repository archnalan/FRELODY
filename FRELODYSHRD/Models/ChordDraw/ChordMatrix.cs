using System;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYSHRD.Models.ChordDraw
{
    /// <summary>
    /// C# port of chordpic's chord-matrix.ts. Holds the editor state in a flat
    /// 1-D array of cells; every mutating operation returns a new instance.
    /// Indices follow chordpic conventions:
    ///   - string is the 0-based string index from the lowest visual column
    ///   - fret is the 0-based fret row (top of fretboard = 0)
    ///   - svguitar's external string numbering is (numStrings - stringIndex) and frets are 1-based
    /// </summary>
    public sealed class ChordMatrix
    {
        public int NumFrets { get; private set; }
        public int NumStrings { get; private set; }

        private MatrixCell[] _cells;
        private ChordEmptyStringState[] _emptyStringStates;

        public ChordMatrix(int numFrets, int numStrings, MatrixCell[]? cells = null, ChordEmptyStringState[]? emptyStringStates = null)
        {
            NumFrets = numFrets;
            NumStrings = numStrings;
            _cells = cells ?? Enumerable.Range(0, numFrets * numStrings).Select(_ => new MatrixCell()).ToArray();
            _emptyStringStates = emptyStringStates ?? Enumerable.Repeat(ChordEmptyStringState.Open, numStrings).ToArray();
        }

        public static ChordMatrix Empty(int numFrets = 5, int numStrings = 6) => new(numFrets, numStrings);

        public static ChordMatrix FromChart(ChordDrawData chart)
        {
            var numFrets = chart.Settings.Frets;
            var numStrings = chart.Settings.Strings;
            if (numFrets <= 0 || numStrings <= 0)
                throw new InvalidOperationException("Cannot create matrix if frets or strings is not known");

            var cells = Enumerable.Range(0, numFrets * numStrings).Select(_ => new MatrixCell()).ToArray();
            var empty = Enumerable.Repeat(ChordEmptyStringState.Open, numStrings).ToArray();

            foreach (var f in chart.Chord.Fingers)
            {
                var stringIndex = Math.Abs(f.String - numStrings);
                if (stringIndex < 0 || stringIndex >= numStrings) continue;

                if (f.Fret == 0)
                {
                    empty[stringIndex] = ChordEmptyStringState.Open;
                }
                else if (f.Fret < 0)
                {
                    empty[stringIndex] = ChordEmptyStringState.Muted;
                }
                else if (f.Fret >= 1 && f.Fret <= numFrets)
                {
                    cells[(f.Fret - 1) * numStrings + stringIndex] = new MatrixCell
                    {
                        State = ChordCellState.Active,
                        Text = f.Text,
                        Color = f.Color,
                        TextColor = f.TextColor,
                        Shape = f.Shape
                    };
                }
            }

            foreach (var b in chart.Chord.Barres)
            {
                var fromIndex = Math.Abs(b.FromString - numStrings);
                var toIndex = Math.Abs(b.ToString - numStrings);
                var lo = Math.Min(fromIndex, toIndex);
                var hi = Math.Max(fromIndex, toIndex);
                if (b.Fret < 1 || b.Fret > numFrets) continue;

                cells[(b.Fret - 1) * numStrings + lo] = new MatrixCell { State = ChordCellState.Left, Text = b.Text, Color = b.Color };
                cells[(b.Fret - 1) * numStrings + hi] = new MatrixCell { State = ChordCellState.Right, Text = b.Text, Color = b.Color };
                for (int i = lo + 1; i < hi; i++)
                {
                    cells[(b.Fret - 1) * numStrings + i] = new MatrixCell { State = ChordCellState.Middle, Text = b.Text, Color = b.Color };
                }
            }

            return new ChordMatrix(numFrets, numStrings, cells, empty);
        }

        public ChordMatrix Clone()
        {
            var cells = _cells.Select(c => c.Clone()).ToArray();
            var empty = (ChordEmptyStringState[])_emptyStringStates.Clone();
            return new ChordMatrix(NumFrets, NumStrings, cells, empty);
        }

        public IReadOnlyList<MatrixCell> Cells => _cells;
        public IReadOnlyList<ChordEmptyStringState> EmptyStringRawStates => _emptyStringStates;

        public ChordMatrix SetNumFrets(int numFrets)
        {
            if (numFrets == NumFrets) return this;
            MatrixCell[] cells;
            if (numFrets < NumFrets)
            {
                cells = _cells.Take(NumStrings * numFrets).Select(c => c.Clone()).ToArray();
            }
            else
            {
                var extra = (numFrets - NumFrets) * NumStrings;
                cells = _cells.Select(c => c.Clone()).Concat(Enumerable.Range(0, extra).Select(_ => new MatrixCell())).ToArray();
            }
            return new ChordMatrix(numFrets, NumStrings, cells, (ChordEmptyStringState[])_emptyStringStates.Clone());
        }

        public ChordMatrix SetNumStrings(int numStrings)
        {
            if (numStrings == NumStrings) return this;

            var newCells = new List<MatrixCell>();
            if (numStrings < NumStrings)
            {
                for (int i = 0; i < _cells.Length; i++)
                {
                    if (i % NumStrings >= numStrings)
                    {
                        var prev = _cells[i];
                        if ((prev.State == ChordCellState.Right || prev.State == ChordCellState.Middle) && newCells.Count > 0)
                        {
                            newCells[newCells.Count - 1] = new MatrixCell { State = ChordCellState.Right };
                        }
                        continue;
                    }
                    newCells.Add(_cells[i].Clone());
                }
            }
            else
            {
                for (int i = 0; i < _cells.Length; i++)
                {
                    newCells.Add(_cells[i].Clone());
                    if (i >= 0 && (i + 1) % NumStrings == 0)
                    {
                        for (int j = 0; j < numStrings - NumStrings; j++)
                            newCells.Add(new MatrixCell());
                    }
                }
            }

            var newEmpty = numStrings < NumStrings
                ? _emptyStringStates.Take(numStrings).ToArray()
                : _emptyStringStates.Concat(Enumerable.Repeat(ChordEmptyStringState.Open, numStrings - NumStrings)).ToArray();

            return new ChordMatrix(NumFrets, numStrings, newCells.ToArray(), newEmpty);
        }

        public MatrixCell Get(int fret, int @string)
        {
            var idx = fret * NumStrings + @string;
            if (idx < 0 || idx >= _cells.Length) throw new ArgumentOutOfRangeException();
            return _cells[idx];
        }

        public ChordCellState GetCellState(int fret, int @string) => Get(fret, @string).State;

        private int GetIndex(int @string, int fret) => fret * NumStrings + @string;

        private ChordMatrix WithCellState(int @string, int fret, ChordCellState state)
        {
            var clone = Clone();
            var idx = clone.GetIndex(@string, fret);
            var current = clone._cells[idx];
            clone._cells[idx] = state == ChordCellState.Inactive
                ? new MatrixCell { State = state }
                : new MatrixCell { State = state, Text = current.Text, Color = current.Color, TextColor = current.TextColor, Shape = current.Shape };
            return clone;
        }

        private ChordMatrix WithSet(int @string, int fret, MatrixCell update)
        {
            var clone = Clone();
            var idx = clone.GetIndex(@string, fret);
            var current = clone._cells[idx];
            clone._cells[idx] = MergeCell(current, update);

            if (IsBarreState(current.State))
            {
                clone.ForEachBarreCell(@string, fret, (s, f, c) =>
                {
                    clone._cells[clone.GetIndex(s, f)] = MergeCell(c, update);
                });
            }
            return clone;
        }

        private static MatrixCell MergeCell(MatrixCell baseCell, MatrixCell update) => new()
        {
            State = baseCell.State,
            Text = update.Text ?? baseCell.Text,
            Color = update.Color ?? baseCell.Color,
            TextColor = update.TextColor ?? baseCell.TextColor,
            Shape = update.Shape ?? baseCell.Shape
        };

        private void ForEachBarreCell(int @string, int fret, Action<int, int, MatrixCell> cb)
        {
            if (!IsBarreState(GetCellState(fret, @string)))
                throw new InvalidOperationException($"No barre at string={@string}, fret={fret}");

            cb(@string, fret, _cells[GetIndex(@string, fret)]);

            var s = @string - 1;
            while (s >= 0 && (GetCellState(fret, s) is ChordCellState.Left or ChordCellState.Middle or ChordCellState.LeftHighlight or ChordCellState.MiddleHighlight))
            {
                cb(s, fret, _cells[GetIndex(s, fret)]);
                s--;
            }
            s = @string + 1;
            while (s < NumStrings && (GetCellState(fret, s) is ChordCellState.Right or ChordCellState.Middle or ChordCellState.RightHighlight or ChordCellState.MiddleHighlight))
            {
                cb(s, fret, _cells[GetIndex(s, fret)]);
                s++;
            }
        }

        public ChordMatrix ToggleEmptyState(int @string)
        {
            var clone = Clone();
            clone._emptyStringStates[@string] = clone._emptyStringStates[@string] == ChordEmptyStringState.Open
                ? ChordEmptyStringState.Muted
                : ChordEmptyStringState.Open;
            return clone;
        }

        public bool IsEmptyString(int @string)
        {
            for (int fret = 0; fret < NumFrets; fret++)
            {
                var s = GetCellState(fret, @string);
                if (s is ChordCellState.Active or ChordCellState.Left or ChordCellState.Right or ChordCellState.Middle)
                    return false;
            }
            return true;
        }

        public ChordEmptyStringState[] GetEmptyStringStates()
        {
            var result = new ChordEmptyStringState[NumStrings];
            for (int i = 0; i < NumStrings; i++)
                result[i] = IsEmptyString(i) ? _emptyStringStates[i] : ChordEmptyStringState.NotEmpty;
            return result;
        }

        public ChordMatrix Toggle(int @string, int fret)
        {
            var state = GetCellState(fret, @string);
            ChordMatrix m = this;
            if (IsBarreState(state))
            {
                m = m.Clone();
                for (int s = 0; s < NumStrings; s++)
                {
                    var idx = m.GetIndex(s, fret);
                    if (IsBarreState(m._cells[idx].State))
                        m._cells[idx] = new MatrixCell { State = ChordCellState.Inactive };
                }
            }
            var newState = m.GetCellState(fret, @string) != ChordCellState.Inactive ? ChordCellState.Inactive : ChordCellState.Active;
            return m.WithCellState(@string, fret, newState);
        }

        public ChordMatrix Text(int @string, int fret, string? text) => WithSet(@string, fret, new MatrixCell { Text = text });
        public ChordMatrix Color(int @string, int fret, string? color) => WithSet(@string, fret, new MatrixCell { Color = color });
        public ChordMatrix SetShape(int @string, int fret, ChordShape shape) => WithSet(@string, fret, new MatrixCell { Shape = shape });

        public ChordMatrix NextShape(int @string, int fret)
        {
            var current = Get(fret, @string).Shape ?? ChordShape.Circle;
            var shapes = Enum.GetValues<ChordShape>();
            var nextIndex = (Array.IndexOf(shapes, current) + 1) % shapes.Length;
            return WithSet(@string, fret, new MatrixCell { Shape = shapes[nextIndex] });
        }

        public ChordMatrix Connect(int fret, int fromString, int toString) => DrawBarre(fret, fromString, toString, highlight: false);
        public ChordMatrix ConnectHighlight(int fret, int fromString, int toString) => DrawBarre(fret, fromString, toString, highlight: true);

        public ChordMatrix ConnectHighlighted()
        {
            if (!_cells.Any(c => c.State is ChordCellState.LeftHighlight or ChordCellState.RightHighlight or ChordCellState.MiddleHighlight))
                return this;

            var clone = Clone();
            for (int i = 0; i < clone._cells.Length; i++)
            {
                var c = clone._cells[i];
                clone._cells[i] = c.State switch
                {
                    ChordCellState.LeftHighlight => new MatrixCell { State = ChordCellState.Left, Text = c.Text, Color = c.Color, TextColor = c.TextColor, Shape = c.Shape },
                    ChordCellState.RightHighlight => new MatrixCell { State = ChordCellState.Right, Text = c.Text, Color = c.Color, TextColor = c.TextColor, Shape = c.Shape },
                    ChordCellState.MiddleHighlight => new MatrixCell { State = ChordCellState.Middle, Text = c.Text, Color = c.Color, TextColor = c.TextColor, Shape = c.Shape },
                    _ => c
                };
            }
            return clone;
        }

        public ChordMatrix ClearHighlights()
        {
            if (!_cells.Any(c => c.State is ChordCellState.LeftHighlight or ChordCellState.RightHighlight or ChordCellState.MiddleHighlight))
                return this;

            var clone = Clone();
            for (int i = 0; i < clone._cells.Length; i++)
            {
                if (clone._cells[i].State is ChordCellState.LeftHighlight or ChordCellState.RightHighlight or ChordCellState.MiddleHighlight)
                    clone._cells[i] = new MatrixCell { State = ChordCellState.Inactive };
            }
            return clone;
        }

        private ChordMatrix DrawBarre(int fret, int fromString, int toString, bool highlight)
        {
            var from = Math.Min(fromString, toString);
            var to = Math.Max(fromString, toString);
            if (fret < 0 || fret > NumFrets - 1) throw new ArgumentOutOfRangeException(nameof(fret));
            if (Math.Abs(from - to) < 1) throw new InvalidOperationException("Strings must be at least 1 apart");
            if (from < 0 || from >= NumStrings) throw new ArgumentOutOfRangeException(nameof(fromString));
            if (to < 0 || to >= NumStrings) throw new ArgumentOutOfRangeException(nameof(toString));

            var clone = Clone();

            for (int s = from; s <= to; s++)
            {
                if (IsBarreState(clone.GetCellState(fret, s)))
                {
                    clone = clone.RemoveBarreAt(fret, s);
                    break;
                }
            }

            int span = to - from;
            for (int i = 0; i <= span; i++)
            {
                var s = from + i;
                ChordCellState state;
                if (i == 0) state = highlight ? ChordCellState.LeftHighlight : ChordCellState.Left;
                else if (i == span) state = highlight ? ChordCellState.RightHighlight : ChordCellState.Right;
                else state = highlight ? ChordCellState.MiddleHighlight : ChordCellState.Middle;

                var idx = clone.GetIndex(s, fret);
                clone._cells[idx] = new MatrixCell { State = state };
            }

            return clone;
        }

        private ChordMatrix RemoveBarreAt(int fret, int @string)
        {
            if (!IsBarreState(GetCellState(fret, @string))) return this;
            var clone = Clone();
            clone.ForEachBarreCell(@string, fret, (s, f, _) =>
            {
                clone._cells[clone.GetIndex(s, f)] = new MatrixCell { State = ChordCellState.Inactive };
            });
            return clone;
        }

        public static bool IsBarreState(ChordCellState state) =>
            state is ChordCellState.Left or ChordCellState.Right or ChordCellState.Middle
                or ChordCellState.LeftHighlight or ChordCellState.RightHighlight or ChordCellState.MiddleHighlight;

        /// <summary>
        /// Convert matrix back to svguitar's Chord format (fingers + barres).
        /// </summary>
        public ChordContent ToChordContent()
        {
            var fingers = new List<ChordFinger>();
            for (int i = 0; i < NumFrets * NumStrings; i++)
            {
                var cell = _cells[i];
                if (cell.State != ChordCellState.Active) continue;
                var stringIndex = i % NumStrings;
                var fretRow = i / NumStrings;
                fingers.Add(new ChordFinger
                {
                    String = Math.Abs(stringIndex - NumStrings),
                    Fret = fretRow + 1,
                    Text = cell.Text,
                    Color = cell.Color,
                    TextColor = cell.TextColor,
                    Shape = cell.Shape
                });
            }

            // Open / muted strings get represented as fingers with fret 0 or -1
            var emptyStates = GetEmptyStringStates();
            for (int s = 0; s < NumStrings; s++)
            {
                if (emptyStates[s] == ChordEmptyStringState.NotEmpty) continue;
                fingers.Add(new ChordFinger
                {
                    String = Math.Abs(s - NumStrings),
                    Fret = emptyStates[s] == ChordEmptyStringState.Open ? 0 : -1
                });
            }

            var barres = new List<ChordBarre>();
            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                var stringIndex = i % NumStrings;
                var fretRow = i / NumStrings;
                var vexString = Math.Abs(stringIndex - NumStrings);
                var vexFret = fretRow + 1;

                if (cell.State == ChordCellState.Left)
                {
                    barres.Add(new ChordBarre
                    {
                        FromString = vexString,
                        ToString = vexString,
                        Fret = vexFret,
                        Text = cell.Text,
                        Color = cell.Color
                    });
                }
                else if (cell.State == ChordCellState.Right && barres.Count > 0)
                {
                    barres[^1].ToString = vexString;
                }
            }

            return new ChordContent { Fingers = fingers, Barres = barres };
        }

        public ChordDrawData ToChart(ChordDrawSettings settings) => new()
        {
            Version = 1,
            Chord = ToChordContent(),
            Settings = settings
        };
    }

    public sealed class MatrixCell
    {
        public ChordCellState State { get; set; } = ChordCellState.Inactive;
        public string? Text { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public ChordShape? Shape { get; set; }

        public MatrixCell Clone() => new()
        {
            State = State,
            Text = Text,
            Color = Color,
            TextColor = TextColor,
            Shape = Shape
        };
    }
}
