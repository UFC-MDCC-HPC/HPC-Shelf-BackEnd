** Tests
Instructions
------------

* sudo apt install nuget
* configurar as credenciais .aws
* criar o grupo hpc-shelf com as devidas permissões de acesso
* a localidade aws está atualmente definida com Virginia USA
* sudo mkdir /home/ubuntu/
* cd /home/ubuntu/
* git clone https://github.com/UFC-MDCC-HPC/HPC-Shelf-BackEnd
* abrir pelo monodevelop o arquivo /home/ubuntu/HPC-Shelf-BackEnd/HPC-Shelf-BackEnd.sln
* recompilar
* #criar um link simbolico com o caminho /home/ubuntu/backend:
*   ln -s /home/ubuntu/HPC-Shelf-BackEnd/HPC-Shelf-BackEnd/scripts/ backend
* cd /home/ubuntu/HPC-Shelf-BackEnd/HPC-Shelf-BackEnd/
* #lançar o servidor xsp4
*   xsp4

