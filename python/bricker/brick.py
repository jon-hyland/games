"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from typing import List
from time import perf_counter
from color import Colors, Color


class Brick:
    """Represents a live, moving brick that has not yet joined the static game matrix.
    It will do so once it's hit bottom and come to rest."""

    def __init__(self, shape_num: int) -> None:
        """Class constructor.  Creates one of seven basic shapes."""
        self.__shape_num: int = shape_num
        if shape_num == 1:
            self.__width: int = 4
            self.__height: int = 4
            self.__grid: List[List[int]] = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[0][2] = 1
            self.__grid[1][2] = 1
            self.__grid[2][2] = 1
            self.__grid[3][2] = 1
            self.__color = Colors.SilverPink
        elif shape_num == 2:
            self.__width = 3
            self.__height = 3
            self.__grid = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[0][1] = 1
            self.__grid[0][2] = 1
            self.__grid[1][2] = 1
            self.__grid[2][2] = 1
            self.__color = Colors.TuftsBlue
        elif shape_num == 3:
            self.__width = 3
            self.__height = 3
            self.__grid = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[2][1] = 1
            self.__grid[0][2] = 1
            self.__grid[1][2] = 1
            self.__grid[2][2] = 1
            self.__color = Colors.ChromeYellow
        elif shape_num == 4:
            self.__width = 2
            self.__height = 2
            self.__grid = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[0][0] = 1
            self.__grid[0][1] = 1
            self.__grid[1][0] = 1
            self.__grid[1][1] = 1
            self.__color = Colors.Independence
        elif shape_num == 5:
            self.__width = 3
            self.__height = 3
            self.__grid = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[1][0] = 1
            self.__grid[2][0] = 1
            self.__grid[0][1] = 1
            self.__grid[1][1] = 1
            self.__color = Colors.ForestGreen
        elif shape_num == 6:
            self.__width = 3
            self.__height = 3
            self.__grid = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[1][1] = 1
            self.__grid[0][2] = 1
            self.__grid[1][2] = 1
            self.__grid[2][2] = 1
            self.__color = Colors.Byzantine
        elif shape_num == 7:
            self.__width = 3
            self.__height = 3
            self.__grid = [[0 for x in range(self.__height)] for y in range(self.__width)]
            self.__grid[0][0] = 1
            self.__grid[1][0] = 1
            self.__grid[1][1] = 1
            self.__grid[2][1] = 1
            self.__color = Colors.Coquelicot
        self.__top_space: int = self.__get_top_space()
        self.__bottom_space: int = self.__get_bottom_space()
        self.__x: int = int((12 - self.__width) / 2)
        self.__y: int = 1 - self.__top_space
        self.__last_drop_time: float = perf_counter()

    @property
    def shape_num(self) -> int:
        """Returns the shape number."""
        return self.__shape_num

    @property
    def width(self) -> int:
        """Returns brick width."""
        return self.__width

    @property
    def height(self) -> int:
        """Returns brick height."""
        return self.__height

    @property
    def grid(self) -> List[List[int]]:
        """Returns brick grid."""
        return self.__grid

    @property
    def color(self) -> Color:
        """Returns brick color."""
        return self.__color

    @property
    def top_space(self) -> int:
        """Returns non-solid spaces at top of brick grid."""
        return self.__top_space

    @property
    def bottom_space(self) -> int:
        """Returns non-solid spaces at bottom of brick grid."""
        return self.__bottom_space

    @property
    def x(self) -> int:
        """Returns X position of brick."""
        return self.__x

    @property
    def y(self) -> int:
        """Returns Y position of brick."""
        return self.__y

    def __get_top_space(self) -> int:
        """Calculates non-solid spaces at top of brick grid."""
        top_space = 0
        for y in range(0, self.__height):
            empty = True
            for x in range(0, self.__width):
                if self.__grid[x][y] == 1:
                    empty = False
            if empty:
                top_space += 1
            else:
                break
        return top_space

    def __get_bottom_space(self) -> int:
        """Calculates non-solid spaces at bottom of brick grid."""
        bottom_space = 0
        for y in reversed(range(0, self.__height)):
            empty = True
            for x in range(0, self.__width):
                if self.__grid[x][y] == 1:
                    empty = False
            if empty:
                bottom_space += 1
            else:
                break
        return bottom_space

    def collision(self, matrix) -> bool:
        """Returns true on brick collision."""
        for x in range(0, self.__width):
            for y in range(0, self.__height):
                matrix_x = x + self.__x
                matrix_y = y + self.__y
                if (self.__grid[x][y] == 1) and (matrix[matrix_x][matrix_y] == 1):
                    return True
        return False

    def move_left(self, matrix) -> None:
        """Moves brick left, prevents collision."""
        self.__x -= 1
        if self.collision(matrix):
            self.__x += 1

    def move_right(self, matrix) -> None:
        """Moves brick right, prevents collision."""
        self.__x += 1
        if self.collision(matrix):
            self.__x -= 1

    def move_down(self, matrix) -> bool:
        """Moves brick down, prevents collision.  Returns true if move would have hit bottom."""
        self.__last_drop_time = perf_counter()
        self.__y += 1
        if self.collision(matrix):
            self.__y -= 1
            return True
        return False

    def is_drop_time(self, interval) -> bool:
        """Returns true if its time to drop brick (gravity)."""
        now = perf_counter()
        elapsed = now - self.__last_drop_time
        drop_time = elapsed >= interval
        return drop_time

    def rotate(self, matrix) -> None:
        """Rotates brick."""

        new_grid = [[0 for x in range(self.__width)] for y in range(self.__height)]
        for x1 in range(0, self.__width):
            for y1 in range(0, self.__height):
                x2 = -y1 + (self.__height - 1)
                y2 = x1
                new_grid[x2][y2] = self.__grid[x1][y1]
        self.__grid = new_grid

        steps = 0
        while self.collision(matrix):
            self.__y += 1
            steps += 1
            if steps >= 3:
                self.__y -= 3
                break

        steps = 0
        while self.collision(matrix):
            self.__y -= 1
            steps += 1
            if steps >= 3:
                self.__y += 3
                break

        steps = 0
        while self.collision(matrix):
            self.__x -= 1
            steps += 1
            if steps >= 3:
                self.__x += 3
                break

        steps = 0
        while self.collision(matrix):
            self.__x += 1
            steps += 1
            if steps >= 3:
                self.__x -= 3
                break
