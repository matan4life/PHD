import numpy as np

# Базовый тип данных для минуций (как в экстракторе)
MINUTIA_DTYPE_BASE = np.dtype([
    ('x', np.float32),
    ('y', np.float32),
    ('theta', np.float32),
    ('quality', np.float32),
    ('type', np.int32)
])

# Расширенный тип с уникальным ID для сравнения
MINUTIA_DTYPE = np.dtype(MINUTIA_DTYPE_BASE.descr + [('id', np.int32)])


def assign_unique_ids(minutiae: np.ndarray, start_id: int = 0) -> tuple:
    """
    Присваивает уникальные ID минуциям для процесса сравнения.

    Args:
        minutiae: Массив минуций с базовым типом данных
        start_id: Начальный ID для присвоения

    Returns:
        Tuple (extended_minutiae, next_id) где:
        - extended_minutiae: массив с добавленными ID
        - next_id: следующий свободный ID
    """
    if len(minutiae) == 0:
        return np.array([], dtype=MINUTIA_DTYPE), start_id

    # Создаем новый массив с расширенным типом данных
    extended = np.zeros(len(minutiae), dtype=MINUTIA_DTYPE)

    # Копируем все поля из исходного массива
    for field in MINUTIA_DTYPE_BASE.names:
        extended[field] = minutiae[field]

    # Присваиваем уникальные ID
    extended['id'] = np.arange(start_id, start_id + len(minutiae), dtype=np.int32)

    return extended, start_id + len(minutiae)
