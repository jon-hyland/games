"""Bricker - A Tetris-like brick game.
Copyright (C) 2017-2020  John Hyland
GNU GENERAL PUBLIC LICENSE Version 3"""

from setuptools import find_packages, setup


with open('requirements.txt') as f:
    requirements = f.read().splitlines()

try:
    with open("version", "r") as file:
        version = file.readline().strip()
except Exception:
    version = "1.0"

try:
    with open("README.rst", "r") as file:
        description = file.read()
except Exception:
    description = ""


setup(
    name="Bricker",
    version=version,
    packages=find_packages(),
    include_package_data=True,
    zip_safe=False,
    install_requires=requirements,
    package_data={
        "": ["*.py", "*.txt", "*.png", "*.ttf"]
    },
    author="John Hyland",
    author_email="jonhyland@hotmail.com",
    description="A Tetris-like brick game.",
    long_description=description,
    long_description_content_type="text/x-rst",
    keywords="game tetris pygame bricker brick",
    license="GNU",
    url="https://github.com/jon-hyland/bricker/",
    project_urls={
        "Source Code": "https://github.com/jon-hyland/bricker/"
    },
    python_requires=">=3.7"
)
