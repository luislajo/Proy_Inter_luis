/**
 * @file Rutas `/user`: registro, listados, perfil, contraseña y borrado (según rol).
 */
import { Router } from 'express';
import { getOneUserByIdOrDni, getAllUsers, getUsersByRol, register, updateUser, deleteUserById, getMe, changeMyPassword} from '../controllers/usersController.js';
import { verifyToken ,authorizeRoles } from '../middlewares/authMiddleware.js';    
const usersRouter = Router();

usersRouter.get('/', verifyToken, authorizeRoles(["Admin", "Trabajador"]), getAllUsers);
usersRouter.get('/rol/:rol', verifyToken, authorizeRoles(["Admin", "Trabajador"]), getUsersByRol);
usersRouter.get('/getOne', verifyToken, authorizeRoles(["Admin", "Trabajador"]), getOneUserByIdOrDni);
usersRouter.get('/getMe', verifyToken, getMe)

usersRouter.post('/registerApp', register);
usersRouter.post('/registerEsc', verifyToken, authorizeRoles(["Admin", "Trabajador"]), register);

usersRouter.put('/changeMyPassword', verifyToken, changeMyPassword);
usersRouter.put('/update', verifyToken, updateUser);
usersRouter.delete('/delete/:id', verifyToken, authorizeRoles(["Admin", "Trabajador"]), deleteUserById);

export default usersRouter;
