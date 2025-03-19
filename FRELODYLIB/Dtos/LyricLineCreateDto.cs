using SongsWithChords.Models;
using System.ComponentModel.DataAnnotations;


namespace SongsWithChords.Dtos
{
	public class LyricLineCreateDto:IValidatableObject
	{
        public Guid? Id { get; set; }
        public long LyricLineOrder { get; set; }
        public Guid? VerseId { get; set; }
        public Guid? ChorusId { get; set; }
        public Guid? BridgeId { get; set; }
        public ICollection<LyricSegment>? LyricSegments { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            int noIdCount = 0;

            if (VerseId == null) noIdCount++;
            if (BridgeId == null) noIdCount++;
            if (ChorusId == null) noIdCount++;

            if (noIdCount == 3)
            {
                yield return new ValidationResult(
                    "One of VerseId, ChorusId, or BridgeId must be provided",
                    new[] { nameof(VerseId), nameof(BridgeId), nameof(ChorusId) });
            }
            if (VerseId != null && BridgeId != null)
            {
                yield return new ValidationResult(
                    "Same Lyric Line cannot belong to both the Verse and the Bridge",
                    new[] { nameof(VerseId), nameof(BridgeId) });
            }
            if (VerseId != null && ChorusId != null)
            {
                yield return new ValidationResult(
                    "Same Lyric Line cannot belong to both the Verse and the Chorus",
                    new[] { nameof(VerseId), nameof(ChorusId) });
            }
            if (BridgeId != null && ChorusId != null)
            {
                yield return new ValidationResult(
                    "Same Lyric Line cannot belong to both the Bridge and the Chorus",
                    new[] { nameof(BridgeId), nameof(ChorusId) });
            }
        }

    }
}
