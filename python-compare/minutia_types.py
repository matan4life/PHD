import numpy as np

# Define base dtype for minutiae from DynamoDB (int32 for coordinates)
MINUTIA_DTYPE_BASE = np.dtype([
    ('x', np.int32),
    ('y', np.int32),
    ('is_termination', bool),
    ('theta', np.float64)
])

# Extended dtype with unique IDs
MINUTIA_DTYPE = np.dtype(MINUTIA_DTYPE_BASE.descr + [('id', np.int32)])


def assign_unique_ids(minutiae: np.ndarray, start_id: int = 0) -> tuple:
    """
    Assigns or shifts unique IDs to minutiae, returns array and next start_id.

    Args:
        minutiae: NumPy array of minutiae.
        start_id: Starting ID for assignment.

    Returns:
        Tuple (minutiae with IDs, next_start_id).
    """
    if 'id' in minutiae.dtype.names:
        minutiae['id'] += start_id
    else:
        extended = np.zeros(len(minutiae), dtype=MINUTIA_DTYPE)
        for field in MINUTIA_DTYPE_BASE.names:
            extended[field] = minutiae[field]
        extended['id'] = np.arange(start_id, start_id + len(minutiae))
        minutiae = extended
    next_start_id = start_id + len(minutiae)
    return minutiae, next_start_id