"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from typing import List, Optional
from random import randint
from brick import Brick
from color import Color


class Matrix:
    """Stores the 10x20 game matrix.  Contains matrix-related game logic."""

    def __init__(self) -> None:
        """Class constructor."""
        self.__width: int = 12     # 10 visible slots, plus border for collision detection
        self.__height: int = 22    # 20 visible slots, plus border for collision detection
        self.__matrix: List[List[int]] = [[0 for x in range(self.__height)] for y in range(self.__width)]
        self.__color: List[List[Color]] = [[Color(0, 0, 0) for x in range(self.__height)] for y in range(self.__width)]
        for x in range(0, 12):
            self.__matrix[x][0] = 1
            self.__matrix[x][21] = 1
        for y in range(0, 22):
            self.__matrix[0][y] = 1
            self.__matrix[11][y] = 1
        self.__brick: Optional[Brick] = None
        self.__next_brick: Optional[Brick] = None

    @property
    def width(self) -> int:
        """Returns width of game matrix."""
        return self.__width

    @property
    def height(self) -> int:
        """Returns height of game matrix."""
        return self.__height

    @property
    def matrix(self) -> List[List[int]]:
        """Returns game matrix."""
        return self.__matrix

    @property
    def color(self) -> List[List[Color]]:
        """Returns color matrix."""
        return self.__color

    @property
    def brick(self) -> Optional[Brick]:
        """Returns current live brick."""
        return self.__brick

    @property
    def next_brick(self) -> Optional[Brick]:
        """Returns next brick."""
        return self.__next_brick

    def new_game(self) -> None:
        """Resets the game."""
        self.__brick = None
        self.__next_brick = None
        self.__matrix = [[0 for x in range(self.__height)] for y in range(self.__width)]
        self.__color = [[Color(0, 0, 0) for x in range(self.__height)] for y in range(self.__width)]
        for x in range(0, 12):
            self.__matrix[x][0] = 1
            self.__matrix[x][21] = 1
        for y in range(0, 22):
            self.__matrix[0][y] = 1
            self.__matrix[11][y] = 1
        self.spawn_brick()

    def spawn_brick(self) -> bool:
        """Spawns a random new brick.  Returns true on collision (game over)."""
        if self.__next_brick is None:
            shape_num = randint(1, 7)
            self.__next_brick = Brick(shape_num)
        self.__brick = self.__next_brick
        shape_num = randint(1, 7)
        self.__next_brick = Brick(shape_num)
        collision = self.__brick.collision(self.__matrix)
        return collision

    def add_brick_to_matrix(self) -> None:
        """Moves resting brick to matrix."""
        if self.__brick is not None:
            for x in range(0, self.__brick.width):
                for y in range(0, self.__brick.height):
                    if self.__brick.grid[x][y] == 1:
                        self.__matrix[x + self.__brick.x][y + self.__brick.y] = 1
                        self.__color[x + self.__brick.x][y + self.__brick.y] = self.__brick.color
        self.__brick = None

    def move_brick_left(self) -> None:
        """Moves brick to the left."""
        if self.__brick is not None:
            self.__brick.move_left(self.__matrix)

    def move_brick_right(self) -> None:
        """ Moves brick to the right. """
        if self.__brick is not None:
            self.__brick.move_right(self.__matrix)

    def move_brick_down(self) -> bool:
        """Moves brick down.  Returns true if brick hits bottom."""
        hit = False
        if self.__brick is not None:
            hit = self.__brick.move_down(self.__matrix)
        return hit

    def rotate_brick(self) -> None:
        """Rotates brick."""
        if self.__brick is not None:
            self.__brick.rotate(self.__matrix)

    def identify_solid_rows(self) -> List[int]:
        """Checks matrix for solid rows, returns list of solid rows to erase."""
        rows_to_erase = []
        for y in range(1, 21):
            solid = True
            for x in range(1, 11):
                if self.__matrix[x][y] != 1:
                    solid = False
            if solid:
                rows_to_erase.append(y)
        return rows_to_erase
