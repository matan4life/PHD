import numpy as np
import logging

logger = logging.getLogger(__name__)

def aggregate_group_scores(group_scores: list, group_size: int) -> tuple:
    """
    Агрегирует оценки сравнения для группы и нормализует их.

    Args:
        group_scores: Список оценок сравнения для группы
        group_size: Размер группы (количество изображений)

    Returns:
        Tuple (normalized_pos, normalized_mea) где:
        - normalized_pos: нормализованная позитивная оценка
        - normalized_mea: нормализованная средняя оценка
    """
    if not group_scores or group_size == 0:
        logger.warning("Empty group scores or zero group size")
        return 0.0, 0.0

    # Фильтруем валидные оценки (не None и не NaN)
    valid_scores = [score for score in group_scores if score is not None and not np.isnan(score)]

    if not valid_scores:
        logger.warning("No valid scores in group")
        return 0.0, 0.0

    # Позитивная оценка - максимальная оценка в группе
    max_score = max(valid_scores)

    # Средняя оценка группы
    mean_score = np.mean(valid_scores)

    # Нормализация: делим на размер группы для учета размера
    # Позитивная оценка остается как есть (максимум)
    normalized_pos = max_score

    # Средняя оценка взвешивается по количеству успешных сравнений
    success_rate = len(valid_scores) / group_size if group_size > 0 else 0
    normalized_mea = mean_score * success_rate

    logger.debug(f"Group aggregation: max={max_score:.4f}, mean={mean_score:.4f}, "
                f"success_rate={success_rate:.4f}, norm_pos={normalized_pos:.4f}, "
                f"norm_mea={normalized_mea:.4f}")

    return normalized_pos, normalized_mea


def evaluate_thresholds(normalized_pos: float, normalized_mea: float) -> bool:
    """
    Оценивает результат сравнения на основе порогов.

    Использует логику из scan3.ipynb - комбинированные пороги для
    позитивной и средней оценок.

    Args:
        normalized_pos: Нормализованная позитивная оценка
        normalized_mea: Нормализованная средняя оценка

    Returns:
        True если совпадение найдено (YES), False иначе (NO)
    """
    # Пороги на основе экспериментальных данных из scan3.ipynb
    POS_THRESHOLD = 50.0    # Порог для позитивной оценки
    MEA_THRESHOLD = 30.0    # Порог для средней оценки

    # Адаптивный порог - если одна оценка очень высокая,
    # можно смягчить требования к другой
    ADAPTIVE_POS_HIGH = 80.0  # Высокий порог для позитивной оценки
    ADAPTIVE_MEA_LOW = 15.0   # Пониженный порог для средней оценки

    # Основная логика: обе оценки должны превышать базовые пороги
    basic_match = (normalized_pos >= POS_THRESHOLD and
                   normalized_mea >= MEA_THRESHOLD)

    # Адаптивная логика: если позитивная оценка очень высокая,
    # можем смягчить требования к средней
    adaptive_match = (normalized_pos >= ADAPTIVE_POS_HIGH and
                      normalized_mea >= ADAPTIVE_MEA_LOW)

    result = basic_match or adaptive_match

    logger.debug(f"Threshold evaluation: pos={normalized_pos:.4f}, mea={normalized_mea:.4f}, "
                f"basic={basic_match}, adaptive={adaptive_match}, result={result}")

    return result
