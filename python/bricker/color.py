"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from typing import Tuple


class Color:
    """Stores RGB color information."""

    def __init__(self, r: int, g: int, b: int) -> None:
        """Class constructor."""
        self.__r = r
        self.__g = g
        self.__b = b

    @property
    def r(self) -> int:
        """Red value."""
        return self.__r

    @property
    def g(self) -> int:
        """Green value."""
        return self.__g

    @property
    def b(self) -> int:
        """Blue value."""
        return self.__b

    @property
    def value(self) -> Tuple[int, int, int]:
        """Returns the RGB values as a Tuple, needed for pygame."""
        return self.__r, self.__g, self.__b


class Colors:
    """Contains some static RGB color definitions."""
    Black = Color(0, 0, 0)
    ErrorBlack = Color(50, 0, 0)
    White = Color(240, 240, 240)
    Gray = Color(25, 25, 25)
    PansyPurple = Color(87, 24, 69)
    PinkRaspberry = Color(144, 12, 62)
    VividCrimson = Color(199, 0, 57)
    PortlandOrange = Color(255, 87, 51)
    FluorescentOrange = Color(255, 195, 0)
    SilverPink = Color(196, 187, 175)
    Independence = Color(73, 88, 103)
    Coquelicot = Color(252, 49, 0)
    ChromeYellow = Color(243, 169, 3)
    Byzantine = Color(170, 56, 168)
    ForestGreen = Color(54, 137, 38)
    TuftsBlue = Color(74, 125, 219)
    TestBack = Color(25, 0, 0)
