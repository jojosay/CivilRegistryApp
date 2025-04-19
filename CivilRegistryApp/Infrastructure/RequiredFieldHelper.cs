using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CivilRegistryApp.Infrastructure
{
    /// <summary>
    /// Helper class to mark required fields with a red asterisk
    /// </summary>
    public static class RequiredFieldHelper
    {
        /// <summary>
        /// Adds a red asterisk to a TextBlock to indicate a required field
        /// </summary>
        /// <param name="textBlock">The TextBlock to modify</param>
        public static void MarkAsRequired(TextBlock textBlock)
        {
            if (textBlock == null)
                return;

            // Check if the TextBlock already has a red asterisk
            if (HasRedAsterisk(textBlock))
                return;

            // Get the original text
            string originalText = textBlock.Text;

            // If the TextBlock has inlines and no direct text, get text from inlines
            if (string.IsNullOrEmpty(originalText) && textBlock.Inlines.Count > 0)
            {
                foreach (var inline in textBlock.Inlines)
                {
                    if (inline is Run run)
                    {
                        originalText += run.Text;
                    }
                }
            }

            // Store the original text in the TextBlock's Tag property
            if (textBlock.Tag == null)
                textBlock.Tag = originalText;

            // Clear existing inlines
            textBlock.Inlines.Clear();

            // Add the original text
            textBlock.Inlines.Add(new Run(originalText));

            // Create a red asterisk
            Run asterisk = new Run
            {
                Text = " *",
                Foreground = new SolidColorBrush(Colors.Red),
                FontWeight = FontWeights.Bold
            };

            // Add the asterisk to the TextBlock
            textBlock.Inlines.Add(asterisk);
        }

        /// <summary>
        /// Adds a red asterisk to multiple TextBlocks to indicate required fields
        /// </summary>
        /// <param name="textBlocks">The TextBlocks to modify</param>
        public static void MarkAsRequired(params TextBlock[] textBlocks)
        {
            if (textBlocks == null)
                return;

            foreach (var textBlock in textBlocks.Where(tb => tb != null))
            {
                MarkAsRequired(textBlock);
            }
        }

        /// <summary>
        /// Checks if a TextBlock already has a red asterisk
        /// </summary>
        /// <param name="textBlock">The TextBlock to check</param>
        /// <returns>True if the TextBlock has a red asterisk, false otherwise</returns>
        private static bool HasRedAsterisk(TextBlock textBlock)
        {
            // Check if any of the inlines is a red asterisk
            return textBlock.Inlines.OfType<Run>().Any(run =>
                run.Text == " *" &&
                run.Foreground is SolidColorBrush brush &&
                brush.Color == Colors.Red);
        }

        /// <summary>
        /// Removes the red asterisk from a TextBlock
        /// </summary>
        /// <param name="textBlock">The TextBlock to modify</param>
        public static void UnmarkAsRequired(TextBlock textBlock)
        {
            if (textBlock == null)
                return;

            // If the original text was stored in the Tag property, restore it
            if (textBlock.Tag is string originalText)
            {
                // Clear existing inlines
                textBlock.Inlines.Clear();

                // Add the original text as a Run
                textBlock.Inlines.Add(new Run(originalText));

                // Clear the tag
                textBlock.Tag = null;
            }
            else
            {
                // Otherwise, just remove any red asterisk inlines
                var asterisks = textBlock.Inlines.OfType<Run>().Where(run =>
                    run.Text == " *" &&
                    run.Foreground is SolidColorBrush brush &&
                    brush.Color == Colors.Red).ToList();

                foreach (var asterisk in asterisks)
                {
                    textBlock.Inlines.Remove(asterisk);
                }
            }
        }

        /// <summary>
        /// Removes the red asterisk from multiple TextBlocks
        /// </summary>
        /// <param name="textBlocks">The TextBlocks to modify</param>
        public static void UnmarkAsRequired(params TextBlock[] textBlocks)
        {
            if (textBlocks == null)
                return;

            foreach (var textBlock in textBlocks.Where(tb => tb != null))
            {
                UnmarkAsRequired(textBlock);
            }
        }
    }
}
