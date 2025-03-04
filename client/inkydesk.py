import io
import requests
from inky.auto import auto
from PIL import Image

# Initialize the InkyWHAT display
inky_display = auto(ask_user=True, verbose=True)
inky_display.set_border(inky_display.RED)

try:
    # Fetch the image
    response = requests.get('http://satellite-5.local:5229/image')
    
    # Check if the request was successful
    if response.status_code == 200:
        # Read the image data
        img = Image.open(io.BytesIO(response.content))

        # Resize the image to fit the InkyWHAT screen
        img = img.resize(inky_display.resolution, resample=Image.LANCZOS)

        # Display the image on the InkyWHAT screen
        inky_display.set_image(img)
        inky_display.show()
    else:
        print(f"Failed to fetch image. Status code: {response.status_code}")
except requests.exceptions.RequestException as e:
    print(f"Error fetching image: {e}")
except Exception as e:
    print(f"An error occurred while displaying the image: {e}")
