﻿using FRELODYAPP.Dtos;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations;


namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class LyricLineCreateDto : IValidatableObject
    {
        public long LyricLineOrder { get; set; }
        public SongSection PartName { get; set; }
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
        public int? RepeatCount { get; set; }
        public string? VerseId { get; set; }
        public string? ChorusId { get; set; }
        public string? BridgeId { get; set; }
        public ICollection<LyricSegmentDto>? LyricSegments { get; set; }

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
