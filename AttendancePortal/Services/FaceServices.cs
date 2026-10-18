using OpenCvSharp;

namespace AttendancePortal.Services;

public class FaceService : IDisposable
{
    // Cosine similarity at or above this counts as "same person".
    // 0.363 is OpenCV's published threshold for this model. Raise it (e.g. 0.45) for stricter matching.
    public const double MatchThreshold = 0.363;

    private readonly FaceDetectorYN _detector;
    private readonly FaceRecognizerSF _recognizer;
    private readonly object _lock = new();

    public FaceService()
    {
        var detectorPath = FindModel("yunet.onnx");
        var recognizerPath = FindModel("face_recognition_sface_2021dec.onnx");

        _detector = FaceDetectorYN.Create(detectorPath, "", new Size(320, 320), 0.9f, 0.3f, 5000);
        _recognizer = FaceRecognizerSF.Create(recognizerPath, "");
    }

    // Looks for a model file in the app's output folder first, then in the project folder.
    private static string FindModel(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Models_AI", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Models_AI", fileName)
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException(
            "Could not find model file '" + fileName + "'. Looked in: " + string.Join(" | ", candidates));
    }
    /// <summary>
    /// Finds the largest face in the image and returns its 128-number fingerprint.
    /// Returns null (and an error message) if no face is found.
    /// </summary>
    public (float[]? Embedding, string? Error) GetEmbedding(byte[] imageBytes)
    {
        lock (_lock)
        {
            using var image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            if (image.Empty())
            {
                return (null, "The image could not be read.");
            }

            _detector.SetInputSize(new Size(image.Width, image.Height));
            using var faces = new Mat();
            _detector.Detect(image, faces);

            if (faces.Empty() || faces.Rows == 0)
            {
                return (null, "No face found. Face the camera in good light and try again.");
            }

            // Each row is one face: x, y, w, h, 5 landmark points, score. Pick the largest by area.
            var best = 0;
            var bestArea = 0f;
            for (var i = 0; i < faces.Rows; i++)
            {
                var area = faces.Get<float>(i, 2) * faces.Get<float>(i, 3);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = i;
                }
            }

            using var faceRow = faces.Row(best);
            using var aligned = new Mat();
            _recognizer.AlignCrop(image, faceRow, aligned);

            using var feature = new Mat();
            _recognizer.Feature(aligned, feature);

            feature.GetArray(out float[] embedding);

            return (embedding, null);
        }
    }

    /// <summary>Cosine similarity between two fingerprints: closer to 1 means more similar.</summary>
    public static double Similarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB) + 1e-12);
    }

    /// <summary>Compares a live fingerprint against stored ones and returns the best match above the threshold.</summary>
    public static (int? StudentId, double Score) FindBestMatch(float[] probe, IEnumerable<(int StudentId, float[] Embedding)> stored)
    {
        int? bestId = null;
        var bestScore = -1.0;

        foreach (var (studentId, embedding) in stored)
        {
            var score = Similarity(probe, embedding);
            if (score > bestScore)
            {
                bestScore = score;
                bestId = studentId;
            }
        }

        return bestScore >= MatchThreshold ? (bestId, bestScore) : (null, bestScore);
    }

    public static byte[] ToBytes(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        var embedding = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }

    public void Dispose()
    {
        _detector.Dispose();
        _recognizer.Dispose();
    }
}