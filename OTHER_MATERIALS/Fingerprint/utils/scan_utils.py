from models.minutia import Minutia


def is_minutia_in_frame(rectangle_width: int,
                        rectangle_height: int,
                        rectangle_center_x: int,
                        rectangle_center_y: int,
                        minutia: Minutia) -> bool:
    return (rectangle_center_y - rectangle_height <= minutia.y <= rectangle_center_y + rectangle_width
            and rectangle_center_x - rectangle_width <= minutia.x <= rectangle_center_x + rectangle_width)


def adjust_minutia_coords_to_frame(center_x: int, center_y: int, minutia: Minutia) -> Minutia:
    return Minutia(minutia.x - center_x, minutia.y - center_y, minutia.theta)

