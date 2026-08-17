namespace CrimeTestValidator.Helpers;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.NumberWithUnit;

public static partial class TextSanitizer
{
	// 1. Strips stage directions like *sigh*, (pauses), [clears throat]
	[GeneratedRegex(@"[\*\(\[].*?[\*\)\]]", RegexOptions.CultureInvariant)]
	private static partial Regex StageDirectionsRegex();

	// 2. Strips Markdown syntax elements
	[GeneratedRegex(@"[`#\*_~>]", RegexOptions.CultureInvariant)]
	private static partial Regex MarkdownRegex();

	// 3. Strips remaining non-alphanumeric punctuation
	[GeneratedRegex(@"[^\w\s]", RegexOptions.CultureInvariant)]
	private static partial Regex PunctuationRegex();

	// 4. Collapses multi-spaces
	[GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
	private static partial Regex WhitespaceRegex();

	/// <summary>
	/// Sanitizes text using Microsoft.Recognizers.Text to extract and canonicalize entities.
	/// </summary>
	public static string Sanitize(string rawText)
	{
		if (string.IsNullOrWhiteSpace(rawText))
			return string.Empty;

		// Step 1: Remove conversational actions & Markdown
		string cleaned = StageDirectionsRegex().Replace(rawText, " ");
		cleaned = MarkdownRegex().Replace(cleaned, " ");

		// Step 2: Extract & replace all numbers, ordinals, and currencies with standard digits
		cleaned = CanonicalizeEntities(cleaned);

		// Step 3: Strip remaining punctuation noise and normalize spaces
		cleaned = PunctuationRegex().Replace(cleaned, " ");
		return WhitespaceRegex().Replace(cleaned.ToLowerInvariant(), " ").Trim();
	}

	private static string CanonicalizeEntities(string text)
	{
		var results = new List<ModelResult>();

		// Recognize Numbers (e.g., "fifty-four" -> 54, "eight million" -> 8000000)
		results.AddRange(NumberRecognizer.RecognizeNumber(text, Culture.English));

		// Recognize Ordinals (e.g., "fifty-fourth" -> 54, "1st" -> 1)
		results.AddRange(NumberRecognizer.RecognizeOrdinal(text, Culture.English));

		// Recognize Currencies (e.g., "$8 million" -> 8000000)
		results.AddRange(NumberWithUnitRecognizer.RecognizeCurrency(text, Culture.English));

		// Sort results BACKWARDS by start index.
		// Replacing text from right-to-left prevents index offsets from shifting pending matches.
		var validEntities = results
				.Where(r => r.Resolution != null && r.Resolution.ContainsKey("value"))
				.OrderByDescending(r => r.End - r.Start)
				.Aggregate(new List<ModelResult>(), (accepted, current) =>
				{
					bool overlaps = accepted.Any(a => current.Start <= a.End && current.End >= a.Start);
					if (!overlaps) accepted.Add(current);
					return accepted;
				});

		// 2. Sort non-overlapping entities right-to-left (Start index descending)
		// Because there are no overlaps, replacing at S2 (where S2 > S1) leaves index S1 100% valid.
		var sortedEntities = validEntities.OrderByDescending(r => r.Start);

		string updatedText = text;
		foreach (var entity in sortedEntities)
		{
			string canonicalValue = entity.Resolution["value"]?.ToString() ?? entity.Text;
			int length = entity.End - entity.Start + 1;

			updatedText = updatedText.Remove(entity.Start, length)
					.Insert(entity.Start, canonicalValue);
		}

		return updatedText;
	}
}
