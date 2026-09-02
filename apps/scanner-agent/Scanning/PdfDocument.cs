using System.Globalization;
using System.IO;
using System.Text;
using ScannerAgent.Errors;

namespace ScannerAgent.Scanning;

// Wraps a single JFIF/JPEG image into a minimal one-page PDF. Used when a
// TWAIN driver accepts a direct-to-PDF file transfer request but never
// actually produces the output file (observed with the Epson DS-530II).
public static class PdfDocument
{
    public static byte[] WrapJpeg(byte[] jpegBytes, int dpi)
    {
        var (width, height, components) = ReadJpegDimensions(jpegBytes);
        var colorSpace = components == 1 ? "/DeviceGray" : "/DeviceRGB";
        var widthPoints = (width * 72.0 / dpi).ToString("F2", CultureInfo.InvariantCulture);
        var heightPoints = (height * 72.0 / dpi).ToString("F2", CultureInfo.InvariantCulture);

        var contentBytes = Encoding.ASCII.GetBytes(
            $"q {widthPoints} 0 0 {heightPoints} 0 0 cm /Im0 Do Q"
        );

        using var buffer = new MemoryStream();
        var offsets = new int[6];

        void WriteAscii(string text) =>
            buffer.Write(Encoding.ASCII.GetBytes(text));

        WriteAscii("%PDF-1.4\n");

        offsets[1] = (int)buffer.Length;
        WriteAscii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = (int)buffer.Length;
        WriteAscii("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[3] = (int)buffer.Length;
        WriteAscii(
            $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthPoints} {heightPoints}] "
                + "/Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n"
        );

        offsets[4] = (int)buffer.Length;
        WriteAscii(
            $"4 0 obj\n<< /Type /XObject /Subtype /Image /Width {width} /Height {height} "
                + $"/ColorSpace {colorSpace} /BitsPerComponent 8 /Filter /DCTDecode "
                + $"/Length {jpegBytes.Length} >>\nstream\n"
        );
        buffer.Write(jpegBytes);
        WriteAscii("\nendstream\nendobj\n");

        offsets[5] = (int)buffer.Length;
        WriteAscii($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
        buffer.Write(contentBytes);
        WriteAscii("\nendstream\nendobj\n");

        var xrefOffset = buffer.Length;
        WriteAscii("xref\n0 6\n0000000000 65535 f \n");

        for (var i = 1; i <= 5; i++)
        {
            WriteAscii($"{offsets[i]:D10} 00000 n \n");
        }

        WriteAscii("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n");
        WriteAscii(xrefOffset.ToString(CultureInfo.InvariantCulture));
        WriteAscii("\n%%EOF");

        return buffer.ToArray();
    }

    private static (int Width, int Height, int Components) ReadJpegDimensions(
        byte[] data
    )
    {
        var i = 2;

        while (i + 8 < data.Length)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            var marker = data[i + 1];
            i += 2;

            if (marker == 0xD8
                || marker == 0xD9
                || marker == 0x01
                || (marker >= 0xD0 && marker <= 0xD7))
            {
                continue;
            }

            var length = (data[i] << 8) | data[i + 1];
            var isStartOfFrame = marker >= 0xC0
                && marker <= 0xCF
                && marker != 0xC4
                && marker != 0xC8
                && marker != 0xCC;

            if (isStartOfFrame)
            {
                var height = (data[i + 3] << 8) | data[i + 4];
                var width = (data[i + 5] << 8) | data[i + 6];
                var components = data[i + 7];

                return (width, height, components);
            }

            if (marker == 0xDA)
            {
                break;
            }

            i += length;
        }

        throw new ScannerOperationException(
            "TWAIN_PDF_CONVERSION_FAILED",
            "Could not read JPEG dimensions to build the PDF output."
        );
    }
}
