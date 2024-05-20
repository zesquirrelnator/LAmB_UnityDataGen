from datasets import load_dataset

dataset = load_dataset("imagefolder", data_dir="C:/Users/georg/Documents/LmWb_Simulation/Assets/Screenshots", drop_labels=True)

dataset.push_to_hub("zesquirrelnator/Optimized-MoveToRedBall")