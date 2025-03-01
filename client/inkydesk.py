import io
import requests
from inky import InkyWHAT
from PIL import Image

# Initialize the InkyWHAT display
display = InkyWHAT('red')

try:
    # Fetch the image
    response = requests.get('http://satellite-5.local:5229/image')
    
    # Check if the request was successful
    if response.status_code == 200:
        # Write the content to a file
        with open('image.png', 'wb') as f:
            f.write(response.content)

        # Load from file
        img = Image.open('image.png').convert('RGB')

        print(f"Image size: {img.size}")

        palette = Image.new('P', (1, 1))
        palette.putpalette([255, 255, 255, 0, 0, 0, 255, 0, 0,] + [0, 0, 0] * 253)

        # Quantize our image using Inky's 3-colour palette
        img = img.quantize(colors=3, palette=palette)

        # Display the image on the InkyWHAT screen
        display.set_border(display.WHITE)
        display.set_image(img)
        display.show()
    else:
        print(f"Failed to fetch image. Status code: {response.status_code}")
except requests.exceptions.RequestException as e:
    print(f"Error fetching image: {e}")
except Exception as e:
    print(f"An error occurred while displaying the image: {e}")
