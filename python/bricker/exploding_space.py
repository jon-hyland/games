"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from random import randint
from color import Color


class ExplodingSpace:
    """Represents an exploding matrix space, used on game over."""

    def __init__(self, x: float, y: float, color: Color) -> None:
        """Class constructor."""
        self.__x = float(x)
        self.__y = float(y)
        self.__color = color
        self.__x_motion = (float(randint(0, 3000)) / 10.0) + 50.0
        self.__y_motion = (float(randint(0, 3000)) / 10.0) + 50.0
        if randint(0, 1) == 1:
            self.__x_motion = -self.__x_motion
        if randint(0, 1) == 1:
            self.__y_motion = -self.__y_motion

    @property
    def x(self) -> float:
        """X location of space."""
        return self.__x

    @x.setter
    def x(self, value: float) -> None:
        """Sets X location of space."""
        self.__x = value

    @property
    def y(self) -> float:
        """Y Location of space."""
        return self.__y

    @y.setter
    def y(self, value: float) -> None:
        """Sets Y location of space."""
        self.__y = value

    @property
    def color(self) -> Color:
        """Color of space."""
        return self.__color

    @property
    def x_motion(self) -> float:
        """X vector of space."""
        return self.__x_motion


    @property
    def y_motion(self) -> float:
        """Y vector of space."""
        return self.__y_motion
