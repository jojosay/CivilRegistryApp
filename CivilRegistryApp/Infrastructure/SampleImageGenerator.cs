using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CivilRegistryApp.Infrastructure
{
    public static class SampleImageGenerator
    {
        public static void GenerateSampleDocumentImages()
        {
            try
            {
                // Define the sample data directory
                string sampleDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleData");

                // Create the directory if it doesn't exist
                if (!Directory.Exists(sampleDataDir))
                {
                    Directory.CreateDirectory(sampleDataDir);
                }

                // Document types
                string[] documentTypes = { "BirthCertificate", "MarriageCertificate", "DeathCertificate", "CENOMAR" };
                string[] sides = { "Front", "Back" };

                // Generate sample images for each document type
                for (int i = 0; i < 30; i++)
                {
                    foreach (var documentType in documentTypes)
                    {
                        foreach (var side in sides)
                        {
                            string fileName = $"{documentType}_{side}_{i}.jpg";
                            string filePath = Path.Combine(sampleDataDir, fileName);

                            // Only generate if the file doesn't exist
                            if (!File.Exists(filePath))
                            {
                                GeneratePlaceholderImage(filePath, documentType, side, i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - this is not critical functionality
                Console.WriteLine($"Error generating sample images: {ex.Message}");
            }
        }

        private static void GeneratePlaceholderImage(string filePath, string documentType, string side, int index)
        {
            // Create a drawing visual
            DrawingVisual drawingVisual = new DrawingVisual();

            // Define image size
            int width = 850;
            int height = 1100; // Letter size

            // Get drawing context
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Choose background color based on document type
                System.Windows.Media.Color backgroundColor;
                System.Windows.Media.Color headerColor;
                System.Windows.Media.Color borderColor = System.Windows.Media.Colors.DarkGray;

                switch (documentType)
                {
                    case "BirthCertificate":
                        backgroundColor = System.Windows.Media.Color.FromRgb(240, 248, 255); // Light blue
                        headerColor = System.Windows.Media.Colors.DarkBlue;
                        break;
                    case "MarriageCertificate":
                        backgroundColor = System.Windows.Media.Color.FromRgb(255, 245, 238); // Light pink/peach
                        headerColor = System.Windows.Media.Color.FromRgb(139, 0, 0); // Dark red
                        break;
                    case "DeathCertificate":
                        backgroundColor = System.Windows.Media.Color.FromRgb(245, 245, 245); // Light gray
                        headerColor = System.Windows.Media.Colors.Black;
                        break;
                    case "CENOMAR":
                        backgroundColor = System.Windows.Media.Color.FromRgb(240, 255, 240); // Light green
                        headerColor = System.Windows.Media.Color.FromRgb(0, 100, 0); // Dark green
                        break;
                    default:
                        backgroundColor = System.Windows.Media.Colors.White;
                        headerColor = System.Windows.Media.Colors.Black;
                        break;
                }

                // Fill background
                drawingContext.DrawRectangle(
                    new SolidColorBrush(backgroundColor),
                    null,
                    new Rect(0, 0, width, height));

                // Draw fancy border
                drawingContext.DrawRectangle(
                    null,
                    new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(borderColor), 3),
                    new Rect(15, 15, width - 30, height - 30));

                // Draw decorative inner border
                drawingContext.DrawRectangle(
                    null,
                    new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(borderColor), 1),
                    new Rect(25, 25, width - 50, height - 50));

                // Draw header background
                drawingContext.DrawRectangle(
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, headerColor.R, headerColor.G, headerColor.B)),
                    null,
                    new Rect(25, 25, width - 50, 100));

                // Draw Republic of the Philippines text
                System.Windows.Media.FormattedText republicText = new System.Windows.Media.FormattedText(
                    "REPUBLIC OF THE PHILIPPINES",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    16,
                    new System.Windows.Media.SolidColorBrush(headerColor),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(
                    republicText,
                    new System.Windows.Point((width - republicText.Width) / 2, 40));

                // Draw Philippine Statistics Authority text
                System.Windows.Media.FormattedText psaText = new System.Windows.Media.FormattedText(
                    "PHILIPPINE STATISTICS AUTHORITY",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Times New Roman"),
                    14,
                    new System.Windows.Media.SolidColorBrush(headerColor),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(
                    psaText,
                    new System.Windows.Point((width - psaText.Width) / 2, 65));

                // Draw document type header
                string documentTitle = documentType switch
                {
                    "BirthCertificate" => "CERTIFICATE OF LIVE BIRTH",
                    "MarriageCertificate" => "CERTIFICATE OF MARRIAGE",
                    "DeathCertificate" => "CERTIFICATE OF DEATH",
                    "CENOMAR" => "CERTIFICATE OF NO MARRIAGE",
                    _ => documentType.ToUpper()
                };

                System.Windows.Media.FormattedText headerText = new System.Windows.Media.FormattedText(
                    documentTitle,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Times New Roman Bold"),
                    22,
                    new System.Windows.Media.SolidColorBrush(headerColor),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(
                    headerText,
                    new System.Windows.Point((width - headerText.Width) / 2, 90));

                // Draw side indicator
                System.Windows.Media.FormattedText sideText = new System.Windows.Media.FormattedText(
                    $"{side.ToUpper()} SIDE",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(
                    sideText,
                    new System.Windows.Point(width - sideText.Width - 30, 30));

                // Draw registry number
                string registryNumber = $"Registry No. {new Random().Next(1000000, 9999999)}";
                System.Windows.Media.FormattedText registryText = new System.Windows.Media.FormattedText(
                    registryNumber,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Courier New"),
                    12,
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(
                    registryText,
                    new System.Windows.Point(30, 140));

                // Draw form fields based on document type
                if (side == "Front")
                {
                    DrawFormFields(drawingContext, documentType, width, height, drawingVisual);
                }
                else
                {
                    // Draw back side content
                    DrawBackSideContent(drawingContext, documentType, width, height, drawingVisual);
                }

                // Draw sample ID at the bottom
                System.Windows.Media.FormattedText idText = new System.Windows.Media.FormattedText(
                    $"Sample Document #{index}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    10,
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(
                    idText,
                    new System.Windows.Point((width - idText.Width) / 2, height - 30));

                // Draw watermark
                System.Windows.Media.FormattedText watermarkText = new System.Windows.Media.FormattedText(
                    "SAMPLE - NOT A REAL DOCUMENT",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Arial Bold"),
                    36,
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 0, 0)),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                // Rotate the watermark
                drawingContext.PushTransform(new System.Windows.Media.RotateTransform(45, width / 2, height / 2));
                drawingContext.DrawText(
                    watermarkText,
                    new System.Windows.Point((width - watermarkText.Width) / 2, (height - watermarkText.Height) / 2));
                drawingContext.Pop();
            }

            // Render to bitmap
            System.Windows.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);

            // Save to file
            System.Windows.Media.Imaging.JpegBitmapEncoder encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderTargetBitmap));

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        private static void DrawFormFields(DrawingContext drawingContext, string documentType, int width, int height, DrawingVisual drawingVisual)
        {
            // Starting position for form fields
            int startY = 160;
            int leftMargin = 40;
            int fieldHeight = 25;
            int spacing = 10;

            // Common fields for all document types
            var random = new Random();

            // Draw form sections based on document type
            switch (documentType)
            {
                case "BirthCertificate":
                    DrawSectionHeader(drawingContext, "CHILD INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Name:", "JUAN DELA CRUZ SANTOS", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Sex:", "MALE", leftMargin, startY, (width - 2 * leftMargin) / 3, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Date of Birth:", "JANUARY 15, 2020", leftMargin + (width - 2 * leftMargin) / 3 + 10, startY, 2 * (width - 2 * leftMargin) / 3 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Place of Birth:", "ST. LUKE'S MEDICAL CENTER, QUEZON CITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing * 2;

                    DrawSectionHeader(drawingContext, "PARENTS INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Father's Name:", "ROBERTO MENDOZA SANTOS", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Mother's Maiden Name:", "MARIA DELA CRUZ REYES", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing * 2;

                    DrawSectionHeader(drawingContext, "BIRTH DETAILS", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Type of Birth:", "SINGLE", leftMargin, startY, (width - 2 * leftMargin) / 2, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Birth Order:", "FIRST", leftMargin + (width - 2 * leftMargin) / 2 + 10, startY, (width - 2 * leftMargin) / 2 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Birth Weight:", "3.2 KG", leftMargin, startY, (width - 2 * leftMargin) / 2, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Birth Attendant:", "PHYSICIAN", leftMargin + (width - 2 * leftMargin) / 2 + 10, startY, (width - 2 * leftMargin) / 2 - 10, fieldHeight, drawingVisual);
                    break;

                case "MarriageCertificate":
                    DrawSectionHeader(drawingContext, "HUSBAND INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Name:", "CARLOS MANUEL GONZALES", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Age:", "28", leftMargin, startY, (width - 2 * leftMargin) / 4, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Citizenship:", "FILIPINO", leftMargin + (width - 2 * leftMargin) / 4 + 10, startY, 3 * (width - 2 * leftMargin) / 4 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Residence:", "123 MAIN ST., MAKATI CITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing * 2;

                    DrawSectionHeader(drawingContext, "WIFE INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Name:", "PATRICIA ANN SANTOS", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Age:", "26", leftMargin, startY, (width - 2 * leftMargin) / 4, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Citizenship:", "FILIPINO", leftMargin + (width - 2 * leftMargin) / 4 + 10, startY, 3 * (width - 2 * leftMargin) / 4 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Residence:", "456 OAK AVE., QUEZON CITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing * 2;

                    DrawSectionHeader(drawingContext, "MARRIAGE DETAILS", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Date of Marriage:", "FEBRUARY 14, 2022", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Place of Marriage:", "ST. JOSEPH PARISH CHURCH, MANILA", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Solemnizing Officer:", "REV. FR. JOSE SANTOS", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    break;

                case "DeathCertificate":
                    DrawSectionHeader(drawingContext, "DECEASED INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Name:", "RICARDO FLORES REYES", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Sex:", "MALE", leftMargin, startY, (width - 2 * leftMargin) / 4, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Age:", "78 YEARS", leftMargin + (width - 2 * leftMargin) / 4 + 10, startY, (width - 2 * leftMargin) / 4 - 10, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Civil Status:", "MARRIED", leftMargin + 2 * (width - 2 * leftMargin) / 4 + 10, startY, 2 * (width - 2 * leftMargin) / 4 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Residence:", "789 PINE RD., CEBU CITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing * 2;

                    DrawSectionHeader(drawingContext, "DEATH DETAILS", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Date of Death:", "MARCH 10, 2023", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Place of Death:", "CEBU DOCTORS' HOSPITAL, CEBU CITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Cause of Death:", "ACUTE MYOCARDIAL INFARCTION", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Manner of Death:", "NATURAL", leftMargin, startY, (width - 2 * leftMargin) / 2, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Autopsy Performed:", "NO", leftMargin + (width - 2 * leftMargin) / 2 + 10, startY, (width - 2 * leftMargin) / 2 - 10, fieldHeight, drawingVisual);
                    break;

                case "CENOMAR":
                    DrawSectionHeader(drawingContext, "CERTIFICATE INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Name:", "MARIA ELENA SANTOS", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Sex:", "FEMALE", leftMargin, startY, (width - 2 * leftMargin) / 4, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Date of Birth:", "JUNE 12, 1995", leftMargin + (width - 2 * leftMargin) / 4 + 10, startY, 3 * (width - 2 * leftMargin) / 4 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Place of Birth:", "MANILA CITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing * 2;

                    DrawSectionHeader(drawingContext, "CERTIFICATE DETAILS", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Purpose:", "MARRIAGE REQUIREMENT", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Date Issued:", "APRIL 5, 2023", leftMargin, startY, (width - 2 * leftMargin) / 2, fieldHeight, drawingVisual);
                    DrawFormField(drawingContext, "Validity:", "6 MONTHS", leftMargin + (width - 2 * leftMargin) / 2 + 10, startY, (width - 2 * leftMargin) / 2 - 10, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Issuing Authority:", "PHILIPPINE STATISTICS AUTHORITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    break;

                default:
                    // Generic form fields for other document types
                    DrawSectionHeader(drawingContext, "DOCUMENT INFORMATION", leftMargin, startY, width - 2 * leftMargin, drawingVisual);
                    startY += 30;

                    DrawFormField(drawingContext, "Document Type:", documentType, leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Date Issued:", "JANUARY 1, 2023", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    startY += fieldHeight + spacing;

                    DrawFormField(drawingContext, "Issuing Office:", "PHILIPPINE STATISTICS AUTHORITY", leftMargin, startY, width - 2 * leftMargin, fieldHeight, drawingVisual);
                    break;
            }

            // Draw certification section at the bottom
            int certificationY = height - 200;
            DrawSectionHeader(drawingContext, "CERTIFICATION", leftMargin, certificationY, width - 2 * leftMargin, drawingVisual);

            System.Windows.Media.FormattedText certText = new System.Windows.Media.FormattedText(
                "This is to certify that the information contained herein is true and correct to the best of my knowledge and belief.",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Times New Roman"),
                11,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(
                certText,
                new System.Windows.Point(leftMargin, certificationY + 30));

            // Draw signature line
            int signatureY = certificationY + 70;
            drawingContext.DrawLine(
                new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black), 1),
                new System.Windows.Point(width / 2 - 100, signatureY),
                new System.Windows.Point(width / 2 + 100, signatureY));

            System.Windows.Media.FormattedText signatureText = new System.Windows.Media.FormattedText(
                "Civil Registrar",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Times New Roman"),
                10,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(
                signatureText,
                new System.Windows.Point(width / 2 - signatureText.Width / 2, signatureY + 5));
        }

        private static void DrawBackSideContent(DrawingContext drawingContext, string documentType, int width, int height, DrawingVisual drawingVisual)
        {
            // Starting position for content
            int startY = 160;
            int leftMargin = 40;
            int rightMargin = 40;
            int contentWidth = width - leftMargin - rightMargin;

            // Draw back side content based on document type
            switch (documentType)
            {
                case "BirthCertificate":
                case "MarriageCertificate":
                case "DeathCertificate":
                case "CENOMAR":
                    // Draw remarks section
                    DrawSectionHeader(drawingContext, "REMARKS", leftMargin, startY, contentWidth, drawingVisual);
                    startY += 30;

                    // Draw a box for remarks
                    drawingContext.DrawRectangle(
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 0, 0, 0)),
                        new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray), 1),
                        new System.Windows.Rect(leftMargin, startY, contentWidth, 150));

                    // Add some sample remarks text
                    System.Windows.Media.FormattedText remarksText = new System.Windows.Media.FormattedText(
                        "This document is not valid without the official seal and signature of the Civil Registrar.",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Times New Roman"),
                        12,
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(
                        remarksText,
                        new System.Windows.Point(leftMargin + 10, startY + 10));

                    startY += 170;

                    // Draw authentication section
                    DrawSectionHeader(drawingContext, "AUTHENTICATION", leftMargin, startY, contentWidth, drawingVisual);
                    startY += 30;

                    // Draw authentication text
                    System.Windows.Media.FormattedText authText = new System.Windows.Media.FormattedText(
                        "This document has been authenticated by the Philippine Statistics Authority (PSA) and is a true copy of the record on file.",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Times New Roman"),
                        12,
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(
                        authText,
                        new System.Windows.Point(leftMargin, startY));

                    startY += 50;

                    // Draw QR code placeholder
                    drawingContext.DrawRectangle(
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                        new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black), 2),
                        new System.Windows.Rect(leftMargin, startY, 100, 100));

                    // Draw QR code text
                    System.Windows.Media.FormattedText qrText = new System.Windows.Media.FormattedText(
                        "QR CODE",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Arial"),
                        14,
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(
                        qrText,
                        new System.Windows.Point(leftMargin + 50 - qrText.Width / 2, startY + 50 - qrText.Height / 2));

                    // Draw verification text
                    System.Windows.Media.FormattedText verifyText = new System.Windows.Media.FormattedText(
                        "Scan this QR code to verify the authenticity of this document.",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Times New Roman"),
                        12,
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(
                        verifyText,
                        new System.Windows.Point(leftMargin + 120, startY + 40));
                    break;

                default:
                    // Generic back side for other document types
                    DrawSectionHeader(drawingContext, "ADDITIONAL INFORMATION", leftMargin, startY, contentWidth, drawingVisual);
                    startY += 30;

                    System.Windows.Media.FormattedText additionalText = new System.Windows.Media.FormattedText(
                        "This is the back side of the document containing additional information and authentication details.",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new System.Windows.Media.Typeface("Times New Roman"),
                        12,
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(
                        additionalText,
                        new System.Windows.Point(leftMargin, startY));
                    break;
            }

            // Draw legal disclaimer at the bottom
            System.Windows.Media.FormattedText disclaimerText = new System.Windows.Media.FormattedText(
                "LEGAL DISCLAIMER: Unauthorized reproduction of this document is punishable by law.",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Arial Bold"),
                10,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkRed),
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(
                disclaimerText,
                new System.Windows.Point((width - disclaimerText.Width) / 2, height - 60));
        }

        private static void DrawSectionHeader(DrawingContext drawingContext, string headerText, int x, int y, int width, DrawingVisual drawingVisual)
        {
            // Draw section header background
            drawingContext.DrawRectangle(
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230)),
                new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray), 1),
                new System.Windows.Rect(x, y, width, 25));

            // Draw section header text
            System.Windows.Media.FormattedText text = new System.Windows.Media.FormattedText(
                headerText,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Arial Bold"),
                12,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(
                text,
                new System.Windows.Point(x + 5, y + (25 - text.Height) / 2));
        }

        private static void DrawFormField(DrawingContext drawingContext, string label, string value, int x, int y, int width, int height, DrawingVisual drawingVisual)
        {
            // Draw field label
            System.Windows.Media.FormattedText labelText = new System.Windows.Media.FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Arial"),
                10,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(
                labelText,
                new System.Windows.Point(x, y));

            // Draw field value background
            drawingContext.DrawRectangle(
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(10, 0, 0, 0)),
                new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray), 1),
                new System.Windows.Rect(x, y + labelText.Height + 2, width, height - labelText.Height - 2));

            // Draw field value
            System.Windows.Media.FormattedText valueText = new System.Windows.Media.FormattedText(
                value,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Arial"),
                11,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

            drawingContext.DrawText(
                valueText,
                new System.Windows.Point(x + 5, y + labelText.Height + 4));
        }
    }
}
