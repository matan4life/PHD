import numpy as np


def aggregate_group_scores(group_scores: list, group_size: int) -> tuple:
    """
    Aggregates comparison scores for a group into two normalized scores.

    Args:
        group_scores: List of scores (0-100) for probe vs galleries in the group.
        group_size: Number of galleries in the group.

    Returns:
        Tuple (normalized_pos, normalized_mea):
        - normalized_pos: Mean of scores >= 50.0 * count of high scores / group_size.
        - normalized_mea: Overall mean * count of high scores / group_size.
    """
    if not group_scores or group_size == 0:
        return 0.0, 0.0

    scores_array = np.array(group_scores)

    count_high = np.sum(scores_array >= 50.0)

    high_scores = scores_array[scores_array >= 50.0]
    positives_mean = np.mean(high_scores) if count_high > 0 else 0.0

    overall_mean = np.mean(scores_array) if len(scores_array) > 0 else 0.0

    group_pos = positives_mean * count_high
    group_mea = overall_mean * count_high

    normalized_pos = round(group_pos, 2) / group_size if group_size > 0 else 0.0
    normalized_mea = round(group_mea, 2) / group_size if group_size > 0 else 0.0

    return normalized_pos, normalized_mea


def evaluate_thresholds(normalized_pos: float, normalized_mea: float, bound1: float = 15.0, bound2: float = 1.0) -> bool:
    """
    Evaluates if aggregated scores pass thresholds.

    Args:
        normalized_pos: Normalized positive score.
        normalized_mea: Normalized mean score.
        bound1: Threshold for normalized_pos (default 15.0).
        bound2: Threshold for normalized_mea (default 1.0).

    Returns:
        True if both pass thresholds, False otherwise.
    """
    return True if normalized_pos >= bound1 and normalized_mea >= bound2 else False