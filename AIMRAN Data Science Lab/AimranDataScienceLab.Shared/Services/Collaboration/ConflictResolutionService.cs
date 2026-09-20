namespace AIMRAN_Data_Science_Lab.Services.Collaboration;

using AIMRAN_Data_Science_Lab.Domain.Collaboration;

/// <summary>
/// Service for detecting and resolving conflicts in concurrent collaborative editing.
/// Uses Operational Transformation (OT) principles to merge concurrent edits.
/// </summary>
public interface IConflictResolutionService
{
    /// <summary>
    /// Detect conflicts between two concurrent edits using OT principles.
    /// </summary>
    Task<ConflictDetectionResult> DetectConflictAsync(LiveEdit edit1, LiveEdit edit2, string currentContent);

    /// <summary>
    /// Resolve conflicts using specified strategy.
    /// </summary>
    Task<string> ResolveConflictAsync(ConflictRecord conflict, ConflictResolutionStrategy strategy);

    /// <summary>
    /// Apply Operational Transformation to merge edits.
    /// </summary>
    Task<string> ApplyOperationalTransformAsync(string baseContent, LiveEdit edit1, LiveEdit edit2);

    /// <summary>
    /// Check if edits overlap in their affected regions.
    /// </summary>
    bool EditsOverlap(LiveEdit edit1, LiveEdit edit2);

    /// <summary>
    /// Transform one edit relative to another (OT function).
    /// </summary>
    LiveEdit TransformEdit(LiveEdit editToTransform, LiveEdit otherEdit);

    /// <summary>
    /// Validate conflict resolution result.
    /// </summary>
    Task<ConflictValidationResult> ValidateResolutionAsync(string originalContent, string resolvedContent, List<LiveEdit> edits);

    /// <summary>
    /// Get recommended resolution strategy for a conflict.
    /// </summary>
    ConflictResolutionStrategy GetRecommendedStrategy(ConflictRecord conflict);
}

/// <summary>
/// Result of conflict detection.
/// </summary>
public class ConflictDetectionResult
{
    public bool HasConflict { get; set; }
    public string ConflictType { get; set; } = "None"; // None, Overlapping, Cascading
    public int OverlapStart { get; set; }
    public int OverlapEnd { get; set; }
    public string? RecommendedResolution { get; set; }
}

/// <summary>
/// Strategies for resolving conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    LastWriteWins,
    FirstWriteWins,
    OperationalTransform,
    ManualReview,
    AutoMerge,
    PreserveBoth
}

/// <summary>
/// Result of conflict validation.
/// </summary>
public class ConflictValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int ContentDifference { get; set; } // Edit distance
}

/// <summary>
/// Implementation of conflict resolution using Operational Transformation.
/// </summary>
public class ConflictResolutionService : IConflictResolutionService
{
    /// <summary>
    /// Detect conflicts between two concurrent edits.
    /// </summary>
    public Task<ConflictDetectionResult> DetectConflictAsync(LiveEdit edit1, LiveEdit edit2, string currentContent)
    {
        var result = new ConflictDetectionResult { HasConflict = false };

        // Check if edits overlap
        if (EditsOverlap(edit1, edit2))
        {
            result.HasConflict = true;
            result.ConflictType = "Overlapping";

            // Calculate overlap region
            result.OverlapStart = Math.Max(
                edit1.Position,
                edit2.Position
            );
            result.OverlapEnd = Math.Min(
                edit1.Position + Math.Max(0, edit1.InsertedText?.Length ?? 0),
                edit2.Position + Math.Max(0, edit2.InsertedText?.Length ?? 0)
            );

            result.RecommendedResolution = DetermineBestResolution(edit1, edit2);
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Resolve conflicts using the specified strategy.
    /// </summary>
    public Task<string> ResolveConflictAsync(ConflictRecord conflict, ConflictResolutionStrategy strategy)
    {
        var result = strategy switch
        {
            ConflictResolutionStrategy.LastWriteWins => ApplyLastWriteWins(conflict),
            ConflictResolutionStrategy.FirstWriteWins => ApplyFirstWriteWins(conflict),
            ConflictResolutionStrategy.OperationalTransform => ApplyOperationalTransformResolution(conflict),
            ConflictResolutionStrategy.AutoMerge => AttemptAutoMerge(conflict),
            ConflictResolutionStrategy.PreserveBoth => PreserveBothEdits(conflict),
            _ => ApplyLastWriteWins(conflict)
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Apply Operational Transformation to merge edits intelligently.
    /// </summary>
    public Task<string> ApplyOperationalTransformAsync(string baseContent, LiveEdit edit1, LiveEdit edit2)
    {
        try
        {
            // Apply first edit
            string intermediate = ApplyEditToContent(baseContent, edit1);

            // Transform second edit relative to first
            var transformedEdit2 = TransformEdit(edit2, edit1);

            // Apply transformed second edit
            string result = ApplyEditToContent(intermediate, transformedEdit2);

            return Task.FromResult(result);
        }
        catch
        {
            // Fall back to last write wins
            return Task.FromResult(edit2.NewContent ?? "");
        }
    }

    /// <summary>
    /// Check if two edits affect overlapping regions of the document.
    /// </summary>
    public bool EditsOverlap(LiveEdit edit1, LiveEdit edit2)
    {
        // Calculate affected regions
        int edit1End = edit1.Position + Math.Max(0, edit1.InsertedText?.Length ?? 0);
        int edit2End = edit2.Position + Math.Max(0, edit2.InsertedText?.Length ?? 0);

        // Check for overlap
        return !(edit1End <= edit2.Position || edit2End <= edit1.Position);
    }

    /// <summary>
    /// Transform one edit relative to another (core OT function).
    /// This adjusts positions when edits affect different parts of the document.
    /// </summary>
    public LiveEdit TransformEdit(LiveEdit editToTransform, LiveEdit otherEdit)
    {
        var transformed = new LiveEdit
        {
            UserId = editToTransform.UserId,
            UserName = editToTransform.UserName,
            ResourceId = editToTransform.ResourceId,
            ResourceType = editToTransform.ResourceType,
            ProjectId = editToTransform.ProjectId,
            InsertedText = editToTransform.InsertedText,
            DeletedLength = editToTransform.DeletedLength,
            PreviousContent = editToTransform.PreviousContent,
            NewContent = editToTransform.NewContent,
            Version = editToTransform.Version
        };

        // If other edit is before this edit, adjust position
        if (otherEdit.Position < editToTransform.Position)
        {
            int insertedLength = otherEdit.InsertedText?.Length ?? 0;
            transformed.Position = editToTransform.Position - otherEdit.DeletedLength + insertedLength;
        }
        // If other edit deletes content before this edit's position
        else if (otherEdit.Position + otherEdit.DeletedLength <= editToTransform.Position)
        {
            transformed.Position = editToTransform.Position - otherEdit.DeletedLength;
        }

        return transformed;
    }

    /// <summary>
    /// Validate that conflict resolution produced a valid result.
    /// </summary>
    public Task<ConflictValidationResult> ValidateResolutionAsync(string originalContent, string resolvedContent, List<LiveEdit> edits)
    {
        var result = new ConflictValidationResult { IsValid = true };

        // Check if content is not null or empty
        if (string.IsNullOrEmpty(resolvedContent))
        {
            result.IsValid = false;
            result.Errors.Add("Resolved content is empty");
            return Task.FromResult(result);
        }

        // Calculate edit distance
        result.ContentDifference = CalculateEditDistance(originalContent, resolvedContent);

        // Check for reasonable content size
        if (resolvedContent.Length > originalContent.Length * 2)
        {
            result.Warnings.Add("Resolved content is significantly larger than original");
        }

        if (resolvedContent.Length < originalContent.Length * 0.5)
        {
            result.Warnings.Add("Resolved content is significantly smaller than original");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Recommend the best resolution strategy for a conflict.
    /// </summary>
    public ConflictResolutionStrategy GetRecommendedStrategy(ConflictRecord conflict)
    {
        // If edits are by same user, use first write wins
        if (conflict.Edit1.UserId == conflict.Edit2.UserId)
            return ConflictResolutionStrategy.FirstWriteWins;

        // If one edit is significantly larger, use last write wins
        var size1 = (conflict.Edit1.NewContent?.Length ?? 0);
        var size2 = (conflict.Edit2.NewContent?.Length ?? 0);
        if (Math.Abs(size1 - size2) > size1 * 0.5)
            return ConflictResolutionStrategy.LastWriteWins;

        // Default to OT for complex scenarios
        return ConflictResolutionStrategy.OperationalTransform;
    }

    // ===== PRIVATE HELPERS =====

    private string DetermineBestResolution(LiveEdit edit1, LiveEdit edit2)
    {
        // If edits don't actually conflict on content, can merge
        if (edit1.Position + (edit1.InsertedText?.Length ?? 0) <= edit2.Position)
            return "CanMerge";

        if (edit2.Position + (edit2.InsertedText?.Length ?? 0) <= edit1.Position)
            return "CanMerge";

        // Otherwise need conflict resolution
        return edit2.CreatedAt > edit1.CreatedAt ? "UseEdit2" : "UseEdit1";
    }

    private string ApplyLastWriteWins(ConflictRecord conflict)
    {
        // The edit with the later timestamp wins
        return conflict.Edit2.CreatedAt > conflict.Edit1.CreatedAt
            ? conflict.Edit2.NewContent ?? ""
            : conflict.Edit1.NewContent ?? "";
    }

    private string ApplyFirstWriteWins(ConflictRecord conflict)
    {
        // The edit with the earlier timestamp wins
        return conflict.Edit1.CreatedAt < conflict.Edit2.CreatedAt
            ? conflict.Edit1.NewContent ?? ""
            : conflict.Edit2.NewContent ?? "";
    }

    private string ApplyOperationalTransformResolution(ConflictRecord conflict)
    {
        try
        {
            return ApplyOperationalTransformAsync(
                conflict.Edit1.PreviousContent ?? "",
                conflict.Edit1,
                conflict.Edit2
            ).Result;
        }
        catch
        {
            return ApplyLastWriteWins(conflict);
        }
    }

    private string AttemptAutoMerge(ConflictRecord conflict)
    {
        // Try to merge if edits don't truly conflict
        if (!EditsOverlap(conflict.Edit1, conflict.Edit2))
        {
            string result = conflict.Edit1.PreviousContent ?? "";
            result = ApplyEditToContent(result, conflict.Edit1);
            result = ApplyEditToContent(result, conflict.Edit2);
            return result;
        }

        // If they do conflict, use last write wins
        return ApplyLastWriteWins(conflict);
    }

    private string PreserveBothEdits(ConflictRecord conflict)
    {
        // Preserve both edits with conflict markers
        var result = new System.Text.StringBuilder(conflict.Edit1.PreviousContent ?? "");

        result.AppendLine("\n<<<<<<< Edit1");
        result.Append(conflict.Edit1.NewContent ?? "");
        result.AppendLine("\n=======");
        result.Append(conflict.Edit2.NewContent ?? "");
        result.AppendLine("\n>>>>>>> Edit2");

        return result.ToString();
    }

    private string ApplyEditToContent(string content, LiveEdit edit)
    {
        if (edit.Position < 0 || edit.Position > content.Length)
            return content;

        var result = new System.Text.StringBuilder();

        // Add content before edit
        result.Append(content.Substring(0, edit.Position));

        // Add inserted text
        if (!string.IsNullOrEmpty(edit.InsertedText))
            result.Append(edit.InsertedText);

        // Add content after deleted portion
        int afterDeletePosition = Math.Min(edit.Position + edit.DeletedLength, content.Length);
        if (afterDeletePosition < content.Length)
            result.Append(content.Substring(afterDeletePosition));

        return result.ToString();
    }

    private int CalculateEditDistance(string s1, string s2)
    {
        // Levenshtein distance calculation
        int[,] dp = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            dp[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            dp[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[s1.Length, s2.Length];
    }
}

/// <summary>
/// Advanced conflict resolution using version vectors and causal ordering.
/// For future use in distributed systems.
/// </summary>
public class VersionVectorConflictResolver : IConflictResolutionService
{
    // TODO: Implement version vector based conflict detection
    // This would track causal relationships between edits
    // and determine if conflicts are truly concurrent

    public Task<ConflictDetectionResult> DetectConflictAsync(LiveEdit edit1, LiveEdit edit2, string currentContent)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }

    public Task<string> ResolveConflictAsync(ConflictRecord conflict, ConflictResolutionStrategy strategy)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }

    public Task<string> ApplyOperationalTransformAsync(string baseContent, LiveEdit edit1, LiveEdit edit2)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }

    public bool EditsOverlap(LiveEdit edit1, LiveEdit edit2)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }

    public LiveEdit TransformEdit(LiveEdit editToTransform, LiveEdit otherEdit)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }

    public Task<ConflictValidationResult> ValidateResolutionAsync(string originalContent, string resolvedContent, List<LiveEdit> edits)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }

    public ConflictResolutionStrategy GetRecommendedStrategy(ConflictRecord conflict)
    {
        throw new NotImplementedException("Version vector implementation pending");
    }
}
