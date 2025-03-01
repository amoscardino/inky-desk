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
        # Read the image data
        img = Image.open(io.BytesIO(response.content))

        print(f"Image fetched successfully. Size: {img.size}")

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
