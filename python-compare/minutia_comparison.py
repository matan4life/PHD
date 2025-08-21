import numpy as np
import math
from concurrent.futures import ProcessPoolExecutor, as_completed
from numba import jit

from minutia_types import MINUTIA_DTYPE


def filter_central_square(minutiae: np.ndarray, center: tuple) -> np.ndarray:
    """
    Filters minutiae to the central 150x150 square around the image centroid.

    Args:
        minutiae: NumPy array of minutiae.
        center: Tuple (center_x, center_y) as integers.

    Returns:
        Filtered minutiae array.
    """
    HALF_SIZE = 75
    center_x, center_y = center
    min_x = center_x - HALF_SIZE
    max_x = center_x + HALF_SIZE
    min_y = center_y - HALF_SIZE
    max_y = center_y + HALF_SIZE
    # Vectorized selection: keep minutiae within square bounds
    mask = ((minutiae['x'] >= min_x) & (minutiae['x'] <= max_x) &
            (minutiae['y'] >= min_y) & (minutiae['y'] <= max_y))
    return minutiae[mask]


def compute_pair_metrics(group_minutiae: np.ndarray) -> tuple:
    """
    Computes distance and angle metrics for all unique pairs in a group.

    Args:
        group_minutiae: NumPy array of minutiae in the group.

    Returns:
        Tuple (dist_cache, angle_cache) as dictionaries.
    """
    if len(group_minutiae) < 2:
        return {}, {}
    idx1, idx2 = np.triu_indices(len(group_minutiae), k=1)
    ids1 = group_minutiae['id'][idx1]
    ids2 = group_minutiae['id'][idx2]
    delta_x = group_minutiae['x'][idx1] - group_minutiae['x'][idx2]
    delta_y = group_minutiae['y'][idx1] - group_minutiae['y'][idx2]
    distances = np.sqrt(delta_x ** 2 + delta_y ** 2)
    angles_rad = np.arctan2(delta_y, delta_x)
    angles_deg = ((angles_rad + 2 * math.pi) % (2 * math.pi)) / math.pi * 180
    mask_reverse = ids1 > ids2
    angles_deg[mask_reverse] = ((angles_rad[mask_reverse] + math.pi + 2 * math.pi) % (2 * math.pi)) / math.pi * 180
    small_ids = np.minimum(ids1, ids2)
    large_ids = np.maximum(ids1, ids2)
    keys = list(zip(small_ids, large_ids))
    dist_cache = dict(zip(keys, distances))
    angle_cache = dict(zip(keys, angles_deg))
    return dist_cache, angle_cache


def retrieve_metric(global_cache: dict, id1: int, id2: int, is_angle: bool = False) -> float:
    """Retrieves a distance or angle metric from the global cache."""
    if id1 == id2:
        return math.nan
    small, large = min(id1, id2), max(id1, id2)
    key = (small, large)
    value = global_cache.get(key, math.nan)
    if is_angle and id1 > id2:
        value = (value + 180) % 360
    return value


def perform_local_comparisons(probe_group: np.ndarray, gallery_group: np.ndarray, global_dist_cache: dict,
                              global_angle_cache: dict) -> list:
    """Performs local comparisons between probe and gallery minutiae groups."""
    local_results = []
    if len(probe_group) == 0 or len(gallery_group) == 0:
        return local_results

    with ProcessPoolExecutor() as executor:
        futures = [
            executor.submit(compute_local_for_probe_minutia, probe_group, gallery_group, global_dist_cache,
                            global_angle_cache, m1_idx)
            for m1_idx in range(len(probe_group))
        ]
        for future in as_completed(futures):
            local_results.extend(future.result())

    return local_results


def compute_local_for_probe_minutia(probe_group: np.ndarray, gallery_group: np.ndarray, global_dist_cache: dict,
                                    global_angle_cache: dict, m1_idx: int) -> list:
    """Computes similarities for one probe minutia against all gallery minutiae."""
    results = []
    m1 = probe_group[m1_idx]
    others1_mask = probe_group['id'] != m1['id']
    others1 = probe_group[others1_mask]
    for m2 in gallery_group:
        others2_mask = gallery_group['id'] != m2['id']
        others2 = gallery_group[others2_mask]
        similarity = compute_square_similarity(m1, m2, others1, others2, global_dist_cache, global_angle_cache)
        if similarity >= 30:
            results.append((m1['id'], m2['id'], similarity))
    return results


@jit(nopython=True)
def perform_greedy_matching(convs: np.ndarray, id1s: np.ndarray, id2s: np.ndarray, max_matches: int) -> int:
    """Numba-optimized greedy matching for pair selection (lower conv better)."""
    num_candidates = len(convs)
    max_id1 = np.max(id1s) + 1
    max_id2 = np.max(id2s) + 1
    used1 = np.zeros(max_id1, dtype=np.bool_)
    used2 = np.zeros(max_id2, dtype=np.bool_)
    score = 0
    for i in range(num_candidates):
        id1 = id1s[i]
        id2 = id2s[i]
        if not used1[id1] and not used2[id2]:
            used1[id1] = True
            used2[id2] = True
            score += 1
            if score >= max_matches:
                break
    return score


def compute_square_similarity(m1: np.record, m2: np.record, others1: np.ndarray, others2: np.ndarray,
                              global_dist_cache: dict, global_angle_cache: dict) -> float:
    """Computes similarity between two minutiae and their neighbors."""
    if len(others1) == 0 or len(others2) == 0:
        return 0.0
    max_matches = min(len(others1), len(others2))
    convs = compute_local_convolutions_vectorized(m1['id'], m2['id'], others1['id'], others2['id'], global_dist_cache,
                                                  global_angle_cache)
    finite_mask = np.isfinite(convs)
    if not np.any(finite_mask):
        return 0.0
    finite_convs = convs[finite_mask]
    num_others1 = len(others1)
    num_others2 = len(others2)
    i1_all_pairs = np.repeat(np.arange(num_others1), num_others2)[finite_mask]
    i2_all_pairs = np.tile(np.arange(num_others2), num_others1)[finite_mask]
    id1s = others1['id'][i1_all_pairs]
    id2s = others2['id'][i2_all_pairs]
    sort_idx = np.argsort(finite_convs)
    convs_sorted = finite_convs[sort_idx]
    id1s_sorted = id1s[sort_idx]
    id2s_sorted = id2s[sort_idx]
    score = perform_greedy_matching(convs_sorted, id1s_sorted, id2s_sorted, max_matches)
    return (score / max_matches) * 100


def compute_local_convolutions_vectorized(m1_id: int, m2_id: int, ids1: np.ndarray, ids2: np.ndarray,
                                          global_dist_cache: dict, global_angle_cache: dict) -> np.ndarray:
    """Vectorized convolution scores for neighbor pairs."""
    dist1 = np.array([retrieve_metric(global_dist_cache, m1_id, id1) for id1 in ids1])
    dist2 = np.array([retrieve_metric(global_dist_cache, m2_id, id2) for id2 in ids2])
    angle1 = np.array([retrieve_metric(global_angle_cache, m1_id, id1, True) for id1 in ids1])
    angle2 = np.array([retrieve_metric(global_angle_cache, m2_id, id2, True) for id2 in ids2])
    diff_dist = np.abs(dist1[:, np.newaxis] - dist2[np.newaxis, :])
    diff_angle = np.abs(angle1[:, np.newaxis] - angle2[np.newaxis, :])
    mask_inf_dist = (diff_dist > 7) | np.isnan(diff_dist)
    mask_inf_angle = (diff_angle > 45) | np.isnan(diff_angle)
    convs = np.full(diff_dist.shape, math.inf)
    valid_mask = ~(mask_inf_dist | mask_inf_angle)
    convs[valid_mask] = (diff_dist[valid_mask] / 7) + (diff_angle[valid_mask] / 45)
    return convs


def perform_global_comparisons(local_results: list, probe_minutiae: np.ndarray, gallery_minutiae: np.ndarray,
                               minutiae_lookup: dict) -> float:
    """Performs global comparisons based on local results."""
    min_len = min(len(probe_minutiae), len(gallery_minutiae))
    if min_len == 0:
        return 0.0

    scores = []

    def process_local_result(local_res: tuple):
        target_id1, target_id2, _ = local_res
        shifted_gallery = shift_gallery_minutiae(gallery_minutiae, target_id1, target_id2, minutiae_lookup)
        match_count = perform_global_matching(probe_minutiae, shifted_gallery)
        score = (match_count / min_len) * 100
        local_scores = [(target_id1, target_id2, score)]

        for match in local_scores:
            sub_id1, sub_id2 = match[0], match[1]
            sub_shifted_gallery = shift_gallery_minutiae(gallery_minutiae, sub_id1, sub_id2, minutiae_lookup)
            sub_match_count = perform_global_matching(probe_minutiae, sub_shifted_gallery)
            sub_score = (sub_match_count / min_len) * 100
            local_scores.append((sub_id1, sub_id2, sub_score))

        return local_scores

    with ProcessPoolExecutor() as executor:
        futures = [executor.submit(process_local_result, res) for res in local_results]
        for future in as_completed(futures):
            scores.extend(future.result())

    if not scores:
        return 0.0
    max_tuple = max(scores, key=lambda x: x[2])
    return max_tuple[2]


@jit(nopython=True)
def perform_global_matching_numba(x1: np.ndarray, y1: np.ndarray, theta1: np.ndarray, term1: np.ndarray,
                                  id1s_array: np.ndarray, x2: np.ndarray, y2: np.ndarray, theta2: np.ndarray,
                                  term2: np.ndarray, id2s_array: np.ndarray, delta_d: float = 15.0,
                                  delta_alpha: float = 12.0) -> int:
    """Numba-optimized global matching computation."""
    delta_x = x1[:, np.newaxis] - x2[np.newaxis, :]
    delta_y = y1[:, np.newaxis] - y2[np.newaxis, :]
    distances = np.sqrt(delta_x ** 2 + delta_y ** 2)
    mask_d = distances <= delta_d

    diff_rad = np.abs(theta1[:, np.newaxis] - theta2[np.newaxis, :])
    diff_rad = np.minimum(diff_rad, 2 * math.pi - diff_rad)
    diff_deg = (diff_rad / math.pi) * 180
    mask_a = diff_deg <= delta_alpha

    term_diff = (term1[:, np.newaxis] != term2[np.newaxis, :]).astype(np.float64)
    scores = (distances / delta_d) + (diff_deg / delta_alpha) + term_diff

    mask = mask_d & mask_a
    scores_flat = scores[mask]
    i1, i2 = np.nonzero(mask)
    id1s_valid = id1s_array[i1]
    id2s_valid = id2s_array[i2]

    sort_idx = np.argsort(scores_flat)
    convs_sorted = scores_flat[sort_idx]
    id1s_sorted = id1s_valid[sort_idx]
    id2s_sorted = id2s_valid[sort_idx]

    max_matches = min(len(x1), len(x2))
    score = perform_greedy_matching(convs_sorted, id1s_sorted, id2s_sorted, max_matches)
    return score


def perform_global_matching(probe_minutiae: np.ndarray, gallery_minutiae: np.ndarray) -> int:
    """Wrapper for global matching, extracting fields for Numba."""
    if len(probe_minutiae) == 0 or len(gallery_minutiae) == 0:
        return 0
    x1 = probe_minutiae['x']
    y1 = probe_minutiae['y']
    theta1 = probe_minutiae['theta']
    term1 = probe_minutiae['is_termination']
    id1s_array = probe_minutiae['id']
    x2 = gallery_minutiae['x']
    y2 = gallery_minutiae['y']
    theta2 = gallery_minutiae['theta']
    term2 = gallery_minutiae['is_termination']
    id2s_array = gallery_minutiae['id']
    match_count = perform_global_matching_numba(x1, y1, theta1, term1, id1s_array, x2, y2, theta2, term2, id2s_array)
    return match_count


def shift_gallery_minutiae(gallery_minutiae: np.ndarray, m1_id: int, m2_id: int, minutiae_lookup: dict) -> np.ndarray:
    """Shifts gallery minutiae by delta between two points."""
    m1 = minutiae_lookup[m1_id]
    m2 = minutiae_lookup[m2_id]
    delta_x = m1['x'] - m2['x']
    delta_y = m1['y'] - m2['y']
    shifted = gallery_minutiae.copy()
    shifted['x'] += delta_x
    shifted['y'] += delta_y
    return shifted


def compare_images(probe_minutiae: np.ndarray, probe_center: tuple, probe_image_id: str, gallery_minutiae: np.ndarray,
                   gallery_center: tuple, gallery_image_id: str, global_dist_cache: dict,
                   global_angle_cache: dict) -> tuple:
    """
    Compares two images, returning score and updated caches.

    Args:
        probe_minutiae: NumPy array of probe minutiae (with IDs).
        probe_center: Tuple (center_x, center_y) as integers.
        probe_image_id: String ID of probe image for cache lookup.
        gallery_minutiae: NumPy array of gallery minutiae (with IDs).
        gallery_center: Tuple (center_x, center_y) as integers.
        gallery_image_id: String ID of gallery image for cache lookup.
        global_dist_cache: Dict {ImageId: {pair: dist}} for distances.
        global_angle_cache: Dict {ImageId: {pair: angle}} for angles.

    Returns:
        Tuple (score, global_dist_cache, global_angle_cache).
    """
    probe_group = filter_central_square(probe_minutiae, probe_center)
    gallery_group = filter_central_square(gallery_minutiae, gallery_center)

    # Check if metrics already cached
    if probe_image_id not in global_dist_cache:
        probe_dist, probe_angle = compute_pair_metrics(probe_group)
        global_dist_cache[probe_image_id] = probe_dist
        global_angle_cache[probe_image_id] = probe_angle
    if gallery_image_id not in global_dist_cache:
        gallery_dist, gallery_angle = compute_pair_metrics(gallery_group)
        global_dist_cache[gallery_image_id] = gallery_dist
        global_angle_cache[gallery_image_id] = gallery_angle

    # Combine caches for comparison
    combined_dist_cache = {}
    combined_angle_cache = {}
    for image_id in [probe_image_id, gallery_image_id]:
        combined_dist_cache.update(global_dist_cache.get(image_id, {}))
        combined_angle_cache.update(global_angle_cache.get(image_id, {}))

    minutiae_lookup = {rec['id']: {f: rec[f] for f in MINUTIA_DTYPE.names} for arr in [probe_minutiae, gallery_minutiae]
                       for rec in arr}
    local_results = perform_local_comparisons(probe_group, gallery_group, combined_dist_cache, combined_angle_cache)
    if not local_results:
        return 0.0, global_dist_cache, global_angle_cache
    score = perform_global_comparisons(local_results, probe_minutiae, gallery_minutiae, minutiae_lookup)
    return score, global_dist_cache, global_angle_cache