using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings
{
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class Marking : IEquatable<Marking>, IComparable<Marking>, IComparable<string>
    {
        [DataField("markingColor")]
        private List<Color> _markingColors = new();

        private Marking()
        {
        }

        public Marking(string markingId,
            List<Color> markingColors)
        {
            MarkingId = markingId;
            _markingColors = markingColors;
        }

        public Marking(string markingId,
            List<Color> markingColors, MarkingCategories category) : this(markingId, markingColors.Count, category)
        {
            MarkingId = markingId;
            _markingColors = markingColors;
        }

        public Marking(Marking marking,
            List<Color> markingColors) : this(marking)
        {
            _markingColors = markingColors;
        }

        public Marking(string markingId,
            IReadOnlyList<Color> markingColors)
            : this(markingId, new List<Color>(markingColors))
        {
        }

        public Marking(Marking marking,
            IReadOnlyList<Color> markingColors)
            : this(marking)
        {
            _markingColors = new(markingColors);
        }

        /// <summary>
        /// Creates a new marking from metadata, setting defaults based on category
        /// </summary>
        /// <param name="markingId"></param>
        /// <param name="colorCount"></param>
        /// <param name="category"></param>
        public Marking(string markingId, int colorCount, MarkingCategories category)
        {
            MarkingId = markingId;
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
                colors.Add(Color.White);
            _markingColors = colors;

            if (category == MarkingCategories.UndergarmentBottom || category == MarkingCategories.UndergarmentTop)
            {
                CanToggleVisible = true;
                OtherCanToggleVisible = true;
            }
            else if (category == MarkingCategories.Genital)
            {
                ShowAtStart = false;
                CanToggleVisible = true;
                OtherCanToggleVisible = false;
                PutOnVerb = "show";
                PutOnVerb2p = "shows";
                TakeOffVerb = "hide";
                TakeOffVerb2p = "hides";
            }
            else
            {
                CanToggleVisible = false;
                OtherCanToggleVisible = false;
                PutOnVerb = "show";
                PutOnVerb2p = "shows";
                TakeOffVerb = "hide";
                TakeOffVerb2p = "hides";
            }
        }

        public Marking(Marking marking, int colorCount) : this(marking)
        {
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
                colors.Add(Color.White);
            _markingColors = colors;
        }

        public Marking(Marking other)
        {
            MarkingId = other.MarkingId;
            _markingColors = new(other.MarkingColors);
            Visible = other.Visible;
            Forced = other.Forced;
            CustomName = other.CustomName;
            CanToggleVisible = other.CanToggleVisible;
            OtherCanToggleVisible = other.OtherCanToggleVisible;
            PutOnVerb = other.PutOnVerb;
            PutOnVerb2p = other.PutOnVerb2p;
            TakeOffVerb = other.TakeOffVerb;
            TakeOffVerb2p = other.TakeOffVerb2p;
            ShowAtStart = other.ShowAtStart;
            RenderOverClothing = other.RenderOverClothing;
        }

        public Marking(MarkingDTO? other)
        {
            if (other == null) return;
            MarkingId = other.MarkingId ?? MarkingId;
            _markingColors = new(other.MarkingColors.Select(x => Color.FromHex(x)) ?? _markingColors);
            ShowAtStart = other.Visible ?? ShowAtStart;
            CustomName = other.CustomName ?? CustomName;
            CanToggleVisible = other.CanToggleVisible ?? CanToggleVisible;
            OtherCanToggleVisible = other.OtherCanToggleVisible ?? OtherCanToggleVisible;
            PutOnVerb = other.PutOnVerb ?? PutOnVerb;
            PutOnVerb2p = other.PutOnVerb2p ?? PutOnVerb2p;
            TakeOffVerb = other.TakeOffVerb ?? TakeOffVerb;
            TakeOffVerb2p = other.TakeOffVerb2p ?? TakeOffVerb2p;
            RenderOverClothing = other.RenderOverClothing ?? RenderOverClothing;
        }

        /// <summary>
        ///     ID of the marking prototype.
        /// </summary>
        [DataField("markingId", required: true)]
        public string MarkingId { get; private set; } = default!;

        /// <summary>
        ///     All colors currently on this marking.
        /// </summary>
        [ViewVariables]
        public IReadOnlyList<Color> MarkingColors => _markingColors;

        /// <summary>
        ///     If this marking is currently visible.
        /// </summary>
        [DataField("visible")]
        public bool Visible = true;

        /// <summary>
        ///     If this marking is can be toggled on or off by the user.
        /// </summary>
        [DataField("customName")]
        public string? CustomName = null;

        /// <summary>
        ///     If this marking is should start enabled.
        /// </summary>
        [DataField("showAtStart")]
        public bool ShowAtStart = true;

        /// <summary>
        ///     If true, this marking should render over worn clothing when supported.
        /// </summary>
        [DataField("renderOverClothing")]
        public bool RenderOverClothing = false;

        /// <summary>
        ///     If this marking is can be toggled on or off by the user.
        /// </summary>
        [DataField("canToggleVisible")]
        public bool CanToggleVisible = false;

        /// <summary>
        ///     If this marking is can be toggled on or off by the other players.
        /// </summary>
        [DataField("otherCanToggleVisible")]
        public bool OtherCanToggleVisible = false;

        /// <summary>
        ///     Verb to use when putting on
        /// </summary>
        [DataField("putOnVerb")]
        public string PutOnVerb = "put on";

        /// <summary>
        ///     Verb to use when taking off
        /// </summary>
        [DataField("takeOffVerb")]
        public string TakeOffVerb = "take off";

        /// <summary>
        ///     Verb to use when putting on (2nd person)
        /// </summary>
        [DataField("putOnVerb2p")]
        public string PutOnVerb2p = "puts on";

        /// <summary>
        ///     Verb to use when taking off (2nd person)
        /// </summary>
        [DataField("takeOffVerb2p")]
        public string TakeOffVerb2p = "takes off";

        /// <summary>
        ///     If this marking should be forcefully applied, regardless of points.
        /// </summary>
        [ViewVariables]
        public bool Forced;

        public void SetColor(int colorIndex, Color color) =>
            _markingColors[colorIndex] = color;

        public void SetColor(Color color)
        {
            for (int i = 0; i < _markingColors.Count; i++)
            {
                _markingColors[i] = color;
            }
        }

        public int CompareTo(Marking? marking)
        {
            if (marking == null)
            {
                return 1;
            }

            return string.Compare(MarkingId, marking.MarkingId, StringComparison.Ordinal);
        }

        public int CompareTo(string? markingId)
        {
            if (markingId == null)
                return 1;

            return string.Compare(MarkingId, markingId, StringComparison.Ordinal);
        }

        public bool Equals(Marking? other)
        {
            if (other == null)
            {
                return false;
            }
            return MarkingId.Equals(other.MarkingId)
                && _markingColors.SequenceEqual(other._markingColors)
                && Visible.Equals(other.Visible)
                && Forced.Equals(other.Forced)
                && CustomName == other.CustomName
                && CanToggleVisible == other.CanToggleVisible
                && OtherCanToggleVisible == other.OtherCanToggleVisible
                && PutOnVerb == other.PutOnVerb
                && PutOnVerb2p == other.PutOnVerb2p
                && TakeOffVerb == other.TakeOffVerb
                && TakeOffVerb2p == other.TakeOffVerb2p
                && ShowAtStart == other.ShowAtStart
                && RenderOverClothing == other.RenderOverClothing;
        }

        public MarkingDTO ToDTO()
        {
            return new MarkingDTO()
            {
                MarkingId = MarkingId,
                CanToggleVisible = CanToggleVisible,
                CustomName = CustomName,
                MarkingColors = _markingColors.Select(x => x.ToHex()).ToList(),
                Visible = ShowAtStart,
                OtherCanToggleVisible = OtherCanToggleVisible,
                PutOnVerb = PutOnVerb,
                PutOnVerb2p = PutOnVerb2p,
                TakeOffVerb = TakeOffVerb,
                TakeOffVerb2p = TakeOffVerb2p,
                RenderOverClothing = RenderOverClothing
            };
        }
    }
}
