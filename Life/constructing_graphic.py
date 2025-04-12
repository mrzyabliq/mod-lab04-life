import matplotlib.pyplot as plt
import os
x = []
y = []
with open('Life/data.txt', 'r') as file:
    for line in file:
        values = line.strip().split()
        x.append(float(values[0].replace(',', '.')))  
        y.append(float(values[1].replace(',', '.')))  

fig, ax = plt.subplots()
ax.plot(x, y)
plt.xlabel('Номер Поколения')
plt.ylabel('Плотность заполнения')
plt.title('Зависимость стабильного поколения от плотности заполнения')
plt.grid(True) 
plt.show()