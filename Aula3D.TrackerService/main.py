import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import socket
import json
import time

# Configuração do UDP
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Caminho para o modelo (baixado anteriormente)
model_path = 'hand_landmarker.task'

# Configuração do MediaPipe Hand Landmarker (Nova API Tasks)
base_options = python.BaseOptions(model_asset_path=model_path)
options = vision.HandLandmarkerOptions(
    base_options=base_options,
    num_hands=1,
    min_hand_detection_confidence=0.5,
    min_hand_presence_confidence=0.5,
    min_tracking_confidence=0.5,
    running_mode=vision.RunningMode.VIDEO,
)

# Inicialização da Webcam
cap = cv2.VideoCapture(0)

print(f"Enviando dados para {UDP_IP}:{UDP_PORT}...")
print(f"Usando modelo: {model_path}")

with vision.HandLandmarker.create_from_options(options) as landmarker:
    try:
        while cap.isOpened():
            success, image = cap.read()
            if not success:
                print("Falha ao capturar imagem da webcam.")
                break

            cv2.imshow('Camera Feed', image)

            # Converte para RGB
            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=image_rgb)

            # Processa o frame com timestamp (exigido para RunningMode.VIDEO)
            timestamp_ms = int(time.time() * 1000)
            results = landmarker.detect_for_video(mp_image, timestamp_ms)

            if results.hand_landmarks:
                # Pegamos a primeira mão detectada
                hand_landmarks = results.hand_landmarks[0]

                # Extrai os 21 pontos (X, Y)
                points = []
                for landmark in hand_landmarks:
                    points.append({
                        "x": round(landmark.x, 4),
                        "y": round(landmark.y, 4)
                    })

                # Empacota em JSON e envia via UDP
                data = json.dumps(points).encode('utf-8')
                sock.sendto(data, (UDP_IP, UDP_PORT))

            # ESC para sair
            if cv2.waitKey(5) & 0xFF == 27:
                break

    finally:
        cap.release()
        cv2.destroyAllWindows()
        sock.close()
